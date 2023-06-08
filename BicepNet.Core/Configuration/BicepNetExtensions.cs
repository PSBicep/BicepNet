using Bicep.Cli.Logging;
using Bicep.Cli.Services;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Registry;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core;
using Bicep.Decompiler;
using Bicep.LanguageServer.Providers;
using BicepNet.Core.Authentication;
using BicepNet.Core.Azure;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using Bicep.Core.Configuration;
using Bicep.Core.Workspaces;
using IOFileSystem = System.IO.Abstractions.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BicepNet.Core.Configuration;
using Bicep.Cli;
using System;

namespace BicepNet.Core;

public static class BicepNetExtensions
{
    public static ServiceCollection AddBicepNet(this ServiceCollection services, ILogger bicepLogger)
    {
        services
            .AddSingleton<BicepNetConfigurationManager>()

            .AddSingleton<INamespaceProvider, DefaultNamespaceProvider>()
            .AddSingleton<IAzResourceTypeLoader, AzResourceTypeLoader>()
            .AddSingleton<IContainerRegistryClientFactory, ContainerRegistryClientFactory>()
            .AddSingleton<ITemplateSpecRepositoryFactory, TemplateSpecRepositoryFactory>()
            .AddSingleton<IModuleDispatcher, ModuleDispatcher>()
            .AddSingleton<IModuleRegistryProvider, DefaultModuleRegistryProvider>()
            .AddSingleton<ITokenCredentialFactory, TokenCredentialFactory>()
            .AddSingleton<IFileResolver, FileResolver>()
            .AddSingleton<IFileSystem, IOFileSystem>()
            .AddSingleton<IConfigurationManager>(s => s.GetRequiredService<BicepNetConfigurationManager>())
            .AddSingleton<IApiVersionProviderFactory, ApiVersionProviderFactory>()
            .AddSingleton<IBicepAnalyzer, LinterAnalyzer>()
            .AddSingleton<IFeatureProviderFactory, FeatureProviderFactory>()
            .AddSingleton<ILinterRulesProvider, LinterRulesProvider>()

            .AddSingleton<BicepCompiler>()
            .AddSingleton<BicepDecompiler>()

            .AddSingleton<AzureResourceProvider>()
            .AddSingleton<IAzResourceProvider>(s => s.GetRequiredService<AzureResourceProvider>())
            .AddSingleton<Workspace>()

            .AddSingleton(bicepLogger)
            .AddSingleton(new IOContext(Console.Out, Console.Error))
            .AddSingleton<IDiagnosticLogger, BicepDiagnosticLogger>()
            .AddSingleton<CompilationService>()

            .AddSingleton<BicepNetTokenCredentialFactory>()
            .Replace(ServiceDescriptor.Singleton<ITokenCredentialFactory>(s => s.GetRequiredService<BicepNetTokenCredentialFactory>()));

        return services;
    }
}
