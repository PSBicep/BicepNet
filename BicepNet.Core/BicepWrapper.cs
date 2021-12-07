using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using BicepNet.Core.Configuration;
using BicepNet.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;

namespace BicepNet.Core
{
    public static partial class BicepWrapper
    {
        public static string BicepVersion { get; } = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion;

        // Services shared between commands
        private static RootConfiguration configuration;
        private static IFileSystem fileSystem;
        private static IModuleDispatcher moduleDispatcher;
        private static IFileResolver fileResolver;
        private static IFeatureProvider featureProvider;
        private static IModuleRegistryProvider moduleRegistryProvider;
        private static IReadOnlyWorkspace workspace;
        private static INamespaceProvider namespaceProvider;
        private static IConfigurationManager configurationManager;
        private static ILogger logger;

        public static void Initialize(ILogger bicepLogger)
        {
            logger = bicepLogger;

            workspace = new Workspace();
            fileSystem = new FileSystem();
            fileResolver = new FileResolver();
            featureProvider = new FeatureProvider();
            configurationManager = new BicepNetConfigurationManager(fileSystem);

            configuration = configurationManager.GetBuiltInConfiguration();

            namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider);

            var tokenCredentialFactory = new TokenCredentialFactory();
            moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
                new ContainerRegistryClientFactory(tokenCredentialFactory),
                new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                featureProvider);
            moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);
        }

        private static (bool success, ICollection<DiagnosticEntry> diagnosticResult) LogDiagnostics(Compilation compilation)
        {
            var diagnosticLogger = new DiagnosticLogger();
            foreach (var (bicepFile, diagnostics) in compilation.GetAllDiagnosticsByBicepFile())
            {
                foreach (var diagnostic in diagnostics)
                {
                    diagnosticLogger.LogDiagnostics(bicepFile.FileUri, diagnostic, bicepFile.LineStarts);
                }
            }
            return (diagnosticLogger.success, diagnosticLogger.diagnosticResult);
        }
    }
}
