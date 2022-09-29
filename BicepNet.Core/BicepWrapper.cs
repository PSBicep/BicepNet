using Azure.Core;
using Azure.Identity;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using BicepNet.Core.Authentication;
using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using BicepNet.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace BicepNet.Core;

public static partial class BicepWrapper
{
    public static string BicepVersion { get; }
    public static string OciCachePath { get; }
    public static string TemplateSpecsCachePath { get; }

    // Services shared between commands
    private static readonly JoinableTaskFactory joinableTaskFactory;
    private static readonly BicepNetTokenCredentialFactory tokenCredentialFactory;
    private static readonly IApiVersionProvider apiVersionProvider;
    private static readonly IReadOnlyWorkspace workspace;
    private static readonly IFileSystem fileSystem;
    private static readonly IFileResolver fileResolver;
    private static readonly IFeatureProvider featureProvider;
    private static readonly BicepNetConfigurationManager configurationManager;
    private static readonly RootConfiguration configuration;
    private static readonly LinterAnalyzer linterAnalyzer;
    private static readonly INamespaceProvider namespaceProvider;
    private static readonly IContainerRegistryClientFactory clientFactory;
    private static readonly IModuleRegistryProvider moduleRegistryProvider;
    private static readonly IModuleDispatcher moduleDispatcher;
    private static readonly IAzResourceTypeLoader azResourceTypeLoader;
    private static readonly AzureResourceProvider azResourceProvider;
    private static ILogger? logger;

    static BicepWrapper()
    {
        joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
        // Create a custom TokenCredentialFactory to allow for token input
        tokenCredentialFactory = new BicepNetTokenCredentialFactory();
        apiVersionProvider = new ApiVersionProvider();
        workspace = new Workspace();
        fileSystem = new FileSystem();
        fileResolver = new FileResolver();
        featureProvider = new FeatureProvider();
        configurationManager = new BicepNetConfigurationManager(fileSystem);
        configuration = configurationManager.GetBuiltInConfiguration();
        linterAnalyzer = new LinterAnalyzer(configuration);
        namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider);
        clientFactory = new ContainerRegistryClientFactory(tokenCredentialFactory);
        moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
            clientFactory,
            new TemplateSpecRepositoryFactory(tokenCredentialFactory),
            featureProvider);
        moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);

        azResourceTypeLoader = new AzResourceTypeLoader();
        azResourceProvider = new AzureResourceProvider(tokenCredentialFactory, fileResolver, moduleDispatcher, configuration, featureProvider, namespaceProvider, apiVersionProvider, linterAnalyzer);
        
        BicepVersion = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion ?? "dev";
        OciCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.Oci);
        TemplateSpecsCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.TemplateSpecs);
    }

    public static void Initialize(ILogger bicepLogger)
    {
        logger = bicepLogger;
    }

    public static void SetAuthentication(string? token = null, string? tenantId = null)
    {
        // User provided a token
        if (!string.IsNullOrEmpty(token))
        {
            tokenCredentialFactory.InteractiveAuthentication = false;
            logger?.LogInformation("Token provided as authentication.");

            // Try to parse JWT for expiry date
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token);
                var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals("exp")).Value;
                var expDateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(tokenExp));

                logger?.LogInformation("Successfully parsed token, expiration date is {expDateTime}.", expDateTime);
                tokenCredentialFactory.Credential = new ExternalTokenCredential(token, expDateTime);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not parse token as JWT, please ensure it is provided in the correct format!", ex);
            }
        }
        else // User did not provide a token - interactive auth
        {
            logger?.LogInformation("Opening interactive browser for authentication...");

            // Since we cannot change the method signatures of the ITokenCredentialFactory, set properties and check them within the class
            tokenCredentialFactory.InteractiveAuthentication = true;
            tokenCredentialFactory.Credential = new InteractiveBrowserCredential(options: new() { AuthorityHost = configuration.Cloud.ActiveDirectoryAuthorityUri });
            tokenCredentialFactory.TokenRequestContext = new TokenRequestContext(new[] { BicepNetTokenCredentialFactory.Scope }, tenantId: tenantId);

            // Get token immediately to not delay the login until a command is executed
            // The token is then stored within the SDK, in the credential object
            tokenCredentialFactory.GetToken();

            tokenCredentialFactory.CreateChain(configuration.Cloud.CredentialPrecedence, configuration.Cloud.ActiveDirectoryAuthorityUri);

            logger?.LogInformation("Authentication successful.");
        }
    }

    public static void ClearAuthentication()
    {
        tokenCredentialFactory.InteractiveAuthentication = false;

        if (tokenCredentialFactory.Credential == null)
        {
            logger?.LogInformation("No stored credential to clear.");
            return;
        }

        tokenCredentialFactory.Credential = null;
        logger?.LogInformation("Cleared stored credential.");
    }

    public static BicepAccessToken? GetAccessToken()
    {
        // Gets the token using the same request context as when connecting
        var token = tokenCredentialFactory.Credential?.GetToken(tokenCredentialFactory.TokenRequestContext, System.Threading.CancellationToken.None);

        if (!token.HasValue)
        {
            logger?.LogWarning("No access token currently stored!");
            return null;
        }

        var tokenValue = token.Value;
        return new BicepAccessToken(tokenValue.Token, tokenValue.ExpiresOn);
    }

    public static BicepConfigInfo GetBicepConfigInfo(BicepConfigScope scope, string path)
    {
        switch (scope)
        {
            case BicepConfigScope.Default:
                return BicepNetConfigurationManager.GetConfigurationInfo();
            // Merged and Local uses the same logic
            case BicepConfigScope.Merged:
            case BicepConfigScope.Local:
                if (path == null)
                {
                    throw new ArgumentException("Path must be provided for this Scope!");
                }
                return configurationManager.GetConfigurationInfo(scope, PathHelper.FilePathToFileUrl(path));
            default:
                throw new ArgumentException("BicepConfigMode not valid!");
        }
    }

    private static bool LogDiagnostics(ImmutableDictionary<BicepFile,ImmutableArray<IDiagnostic>> diagnosticsByBicepFile)
    {
        bool success = true;
        foreach (var (bicepFile, diagnostics) in diagnosticsByBicepFile)
        {
            foreach (var diagnostic in diagnostics)
            {
                success = diagnostic.Level != DiagnosticLevel.Error;
                LogDiagnostic(bicepFile.FileUri, diagnostic, bicepFile.LineStarts);
            }
        }
        return success;
    }
    private static string GetDiagnosticsOutput(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
    {
        var localPath = fileUri.LocalPath;
        var position = TextCoordinateConverter.GetPosition(lineStarts, diagnostic.Span.Position);
        var line = position.line;
        var character = position.character;
        var level = diagnostic.Level;
        var code = diagnostic.Code;
        var message = diagnostic.Message;

        var codeDescription = diagnostic.Uri is null ? string.Empty : $" [{diagnostic.Uri.AbsoluteUri}]";

        return $"{localPath}({line},{character}) : {level} {code}: {message}{codeDescription}";
    }
    private static void LogDiagnostic(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
    {
        var message = GetDiagnosticsOutput(fileUri, diagnostic, lineStarts);

        switch (diagnostic.Level)
        {
            case DiagnosticLevel.Off:
                break;
            case DiagnosticLevel.Info:
                logger?.LogInformation("{message}", message);
                break;
            case DiagnosticLevel.Warning:
                logger?.LogWarning("{message}", message);
                break;
            case DiagnosticLevel.Error:
                logger?.LogError("{message}", message);
                break;
            default:
                break;
        }

        // Increment counters
        if (diagnostic.Level == DiagnosticLevel.Warning) { WarningCount++; }
        if (diagnostic.Level == DiagnosticLevel.Error) { ErrorCount++; }
    }
}
