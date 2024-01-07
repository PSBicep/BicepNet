using Bicep.Cli;
using Bicep.Cli.Helpers;
using Bicep.Cli.Logging;
using Bicep.Cli.Services;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.Providers;
using BicepNet.Core.Authentication;
using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace BicepNet.Core;

public static class BicepNetExtensions
{
    public static ServiceCollection AddBicepNet(this ServiceCollection services, ILogger bicepLogger)
    {
        services
            .AddSingleton<BicepNetConfigurationManager>()
            .AddBicepCore()
            .AddBicepDecompiler()
            .AddBicepparamDecompiler()

            .AddSingleton<AzureResourceProvider>()
            .AddSingleton<IAzResourceProvider>(s => s.GetRequiredService<AzureResourceProvider>())
            .AddSingleton<Workspace>()

            .AddSingleton(bicepLogger)
            .AddSingleton(new IOContext(Console.Out, Console.Error))
            .AddSingleton<DiagnosticLogger>()
            .AddSingleton<CompilationService>()

            .AddSingleton<BicepNetTokenCredentialFactory>()
            .Replace(ServiceDescriptor.Singleton<ITokenCredentialFactory>(s => s.GetRequiredService<BicepNetTokenCredentialFactory>()));

        return services;
    }
}
