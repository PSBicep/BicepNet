using Bicep.Core;
using Bicep.Core.Configuration;
using Bicep.Core.Extensions;
using Bicep.Core.Json;
using BicepNet.Core.Models;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Security;
using System.Text.Json;

namespace BicepNet.Core.Configuration;

public class BicepNetConfigurationManager : ConfigurationManager, IConfigurationManager
{
    public static string BuiltInConfigurationResourceName { get; } = "BicepNet.Core.Configuration.bicepconfig.json";
    
    private static readonly JsonElement BuiltInConfigurationElement = GetBuildInConfigurationElement();

    private static readonly Lazy<RootConfiguration> BuiltInConfigurationLazy =
        new(() => RootConfiguration.Bind(BuiltInConfigurationElement));

    //private readonly ConcurrentDictionary<Uri, (RootConfiguration? config, DiagnosticBuilder.DiagnosticBuilderDelegate? loadError)> configFileUriToLoadedConfigCache = new();
    //private readonly ConcurrentDictionary<Uri, ConfigLookupResult> templateUriToConfigUriCache = new();
    private readonly IFileSystem fileSystem;

    public BicepNetConfigurationManager(IFileSystem fileSystem) : base(fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public static RootConfiguration GetBuiltInConfiguration() => BuiltInConfigurationLazy.Value;
    private static JsonElement GetBuildInConfigurationElement()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(BuiltInConfigurationResourceName);

        if (stream is null)
        {
            throw new InvalidOperationException("Could not get manifest resource stream for built-in configuration.");
        }

        return JsonElementFactory.CreateElement(stream);
    }
    private string? DiscoverConfigurationFile(string? currentDirectory)
    {
        while (!string.IsNullOrEmpty(currentDirectory))
        {
            var configurationPath = fileSystem.Path.Combine(currentDirectory, LanguageConstants.BicepConfigurationFileName);

            if (fileSystem.File.Exists(configurationPath))
            {
                return configurationPath;
            }

            try
            {
                // Catching Directory.GetParent alone because it is the only one that throws IO related exceptions.
                // Path.Combine only throws ArgumentNullException which indicates a bug in our code.
                // File.Exists will not throw exceptions regardless the existence of path or if the user has permissions to read the file.
                currentDirectory = this.fileSystem.Directory.GetParent(currentDirectory)?.FullName;
            }
            catch (Exception exception)
            {
                if (exception is IOException || exception is UnauthorizedAccessException || exception is SecurityException)
                {
                    // TODO: add telemetry here so that users can understand if there's an issue finding Bicep config.
                    // The exception could happen in senarios where users may not have read permission on the parent folder.
                    // We should not throw ConfigurationException in such cases since it will block compilation.
                    return null;
                }
            }
        }

        return null;
    }

    // Default config
    public static BicepConfigInfo GetConfigurationInfo()
    {
        var builtInStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(BuiltInConfigurationResourceName);
        if(builtInStream is null)
        {
            throw new InvalidOperationException("Could not get manifest resource stream for built-in configuration.");
        }
        using StreamReader reader = new(builtInStream);
        string result = reader.ReadToEnd();
        return new BicepConfigInfo("Default", result);
    }
    public BicepConfigInfo GetConfigurationInfo(BicepConfigScope mode, Uri sourceFileUri)
    {
        var configurationPath = DiscoverConfigurationFile(fileSystem.Path.GetDirectoryName(sourceFileUri.LocalPath));

        if (configurationPath is null)
        {
            throw new ArgumentException("No valid configuration file found!");
        }

        switch (mode)
        {
            case BicepConfigScope.Merged:
                var fileStream = fileSystem.FileStream.Create(configurationPath, FileMode.Open, FileAccess.Read);
                string content = BuiltInConfigurationElement.Merge(JsonElementFactory.CreateElement(fileStream)).ToFormattedString();
                return new BicepConfigInfo(configurationPath, content);
            case BicepConfigScope.Default:
                return GetConfigurationInfo();
            case BicepConfigScope.Local:
                return new BicepConfigInfo(configurationPath, File.ReadAllText(configurationPath));
            default:
                throw new ArgumentException("BicepConfigMode not valid!");
        }
    }

    //// From Bicep.Core:
    //public RootConfiguration GetConfiguration(Uri sourceFileUri)
    //{
    //    var (config, diagnosticBuilders) = GetConfigurationFromCache(sourceFileUri);
    //    return WithLoadDiagnostics(config, diagnosticBuilders);
    //}
    //public void PurgeCache()
    //{
    //    PurgeLookupCache();
    //    configFileUriToLoadedConfigCache.Clear();
    //}
    //public void PurgeLookupCache() => templateUriToConfigUriCache.Clear();
    //public (RootConfiguration prevConfiguration, RootConfiguration newConfiguration)? RefreshConfigCacheEntry(Uri configUri)
    //{
    //    (RootConfiguration, RootConfiguration)? returnVal = null;
    //    configFileUriToLoadedConfigCache.AddOrUpdate(configUri, LoadConfiguration, (uri, prev) => {
    //        var reloaded = LoadConfiguration(uri);
    //        if (prev.config is { } prevConfig && reloaded.Item1 is { } newConfig)
    //        {
    //            returnVal = (prevConfig, newConfig);
    //        }
    //        return reloaded;
    //    });

    //    return returnVal;
    //}
    //public void RemoveConfigCacheEntry(Uri configUri)
    //{
    //    if (configFileUriToLoadedConfigCache.TryRemove(configUri, out _))
    //    {
    //        // If a config file has been removed from a workspace, the lookup cache is no longer valid.
    //        PurgeLookupCache();
    //    }
    //}
    //private (RootConfiguration, List<DiagnosticBuilder.DiagnosticBuilderDelegate>) GetConfigurationFromCache(Uri sourceFileUri)
    //{
    //    List<DiagnosticBuilder.DiagnosticBuilderDelegate> diagnostics = new();

    //    var (configFileUri, lookupDiagnostic) = templateUriToConfigUriCache.GetOrAdd(sourceFileUri, LookupConfiguration);
    //    if (lookupDiagnostic is not null)
    //    {
    //        diagnostics.Add(lookupDiagnostic);
    //    }

    //    if (configFileUri is not null)
    //    {
    //        var (config, loadError) = configFileUriToLoadedConfigCache.GetOrAdd(configFileUri, LoadConfiguration);
    //        if (loadError is not null)
    //        {
    //            diagnostics.Add(loadError);
    //        }

    //        if (config is not null)
    //        {
    //            return (config, diagnostics);
    //        }
    //    }

    //    return (GetDefaultConfiguration(), diagnostics);
    //}
    //private static RootConfiguration WithLoadDiagnostics(RootConfiguration configuration, List<DiagnosticBuilder.DiagnosticBuilderDelegate> diagnostics)
    //{
    //    if (diagnostics.Count > 0)
    //    {
    //        return new(configuration.Cloud, configuration.ModuleAliases, configuration.Analyzers, configuration.CacheRootDirectory, configuration.ExperimentalFeaturesEnabled, configuration.ConfigurationPath, diagnostics);
    //    }

    //    return configuration;
    //}
    //private static RootConfiguration GetDefaultConfiguration() => IConfigurationManager.GetBuiltInConfiguration();
    //private (RootConfiguration?, DiagnosticBuilder.DiagnosticBuilderDelegate?) LoadConfiguration(Uri configurationUri)
    //{
    //    try
    //    {
    //        using var stream = fileSystem.FileStream.Create(configurationUri.LocalPath, FileMode.Open, FileAccess.Read);
    //        var element = IConfigurationManager.BuiltInConfigurationElement.Merge(JsonElementFactory.CreateElement(stream));

    //        return (RootConfiguration.Bind(element, configurationUri.LocalPath), null);
    //    }
    //    catch (ConfigurationException exception)
    //    {
    //        return (null, x => x.InvalidBicepConfigFile(configurationUri.LocalPath, exception.Message));
    //    }
    //    catch (JsonException exception)
    //    {
    //        return (null, x => x.UnparsableBicepConfigFile(configurationUri.LocalPath, exception.Message));
    //    }
    //    catch (Exception exception)
    //    {
    //        return (null, x => x.UnloadableBicepConfigFile(configurationUri.LocalPath, exception.Message));
    //    }
    //}
    //private ConfigLookupResult LookupConfiguration(Uri sourceFileUri)
    //{
    //    DiagnosticBuilder.DiagnosticBuilderDelegate? lookupDiagnostic = null;
    //    if (sourceFileUri.Scheme == Uri.UriSchemeFile)
    //    {
    //        string? currentDirectory = fileSystem.Path.GetDirectoryName(sourceFileUri.LocalPath);
    //        while (!string.IsNullOrEmpty(currentDirectory))
    //        {
    //            var configurationPath = this.fileSystem.Path.Combine(currentDirectory, LanguageConstants.BicepConfigurationFileName);

    //            if (this.fileSystem.File.Exists(configurationPath))
    //            {
    //                return new(PathHelper.FilePathToFileUrl(configurationPath), lookupDiagnostic);
    //            }

    //            try
    //            {
    //                // Catching Directory.GetParent alone because it is the only one that throws IO related exceptions.
    //                // Path.Combine only throws ArgumentNullException which indicates a bug in our code.
    //                // File.Exists will not throw exceptions regardless the existence of path or if the user has permissions to read the file.
    //                currentDirectory = this.fileSystem.Directory.GetParent(currentDirectory)?.FullName;
    //            }
    //            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
    //            {
    //                // The exception could happen in senarios where users may not have read permission on the parent folder.
    //                // We should not throw ConfigurationException in such cases since it will block compilation.
    //                lookupDiagnostic = x => x.PotentialConfigDirectoryCouldNotBeScanned(currentDirectory, exception.Message);
    //                break;
    //            }
    //        }
    //    }

    //    return new(null, lookupDiagnostic);
    //}
    //private record ConfigLookupResult(Uri? configFileUri = null, DiagnosticBuilder.DiagnosticBuilderDelegate? lookupDiagnostic = null);
}
