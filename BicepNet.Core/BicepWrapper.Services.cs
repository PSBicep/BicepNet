using Bicep.Core;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Bicep.Decompiler;
using Bicep.LanguageServer.Providers;
using BicepNet.Core.Authentication;
using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    private static IServiceCollection ConfigureServices()
            => new ServiceCollection()
                .AddSingleton<INamespaceProvider, DefaultNamespaceProvider>()
                .AddSingleton<IAzResourceTypeLoader, AzResourceTypeLoader>()
                .AddSingleton<IContainerRegistryClientFactory, ContainerRegistryClientFactory>()
                .AddSingleton<ITemplateSpecRepositoryFactory, TemplateSpecRepositoryFactory>()
                .AddSingleton<IModuleDispatcher, ModuleDispatcher>()
                .AddSingleton<IModuleRegistryProvider, DefaultModuleRegistryProvider>()
                .AddSingleton<BicepNetTokenCredentialFactory>()
                .AddSingleton<ITokenCredentialFactory>(s => s.GetRequiredService<BicepNetTokenCredentialFactory>())
                .AddSingleton<IFileResolver, FileResolver>()
                .AddSingleton<IFileSystem, FileSystem>()
                .AddSingleton<BicepNetConfigurationManager>()
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
                ;
}
