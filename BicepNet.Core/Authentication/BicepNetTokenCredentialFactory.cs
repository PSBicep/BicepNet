using Azure.Core;
using Azure.Identity;
using Bicep.Core.Configuration;
using Bicep.Core.Registry.Auth;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BicepNet.Core.Authentication
{
    public class BicepNetTokenCredentialFactory : ITokenCredentialFactory
    {
        public static string Scope { get; } = "https://management.azure.com/.default";
        internal TokenCredential? Credential { get; set; }
        internal Uri? AuthorityUri { get; private set; }

        public TokenCredential CreateChain(IEnumerable<CredentialType> credentialPrecedence, Uri authorityUri)
        {
            AuthorityUri = authorityUri;

            List<TokenCredential> tokenCredentials = new();

            if (BicepWrapper.ExternalCredential != null)
            {
                Credential = BicepWrapper.ExternalCredential;
                return Credential;
            }
            else if (BicepWrapper.InteractiveAuthentication)
            {
                Credential = new InteractiveBrowserCredential(options: new () { AuthorityHost = authorityUri });
                return Credential;
            }

            if (!credentialPrecedence.Any())
            {
                throw new ArgumentException($"At least one credential type must be provided.");
            }

            return new ChainedTokenCredential(credentialPrecedence.Select(credentialType => CreateSingle(credentialType, authorityUri)).ToArray());
        }

        public TokenCredential CreateSingle(CredentialType credentialType, Uri authorityUri)
        {
            switch (credentialType)
            {
                case CredentialType.Environment:
                    return Credential = new EnvironmentCredential(new() { AuthorityHost = authorityUri });
                case CredentialType.ManagedIdentity:
                    return Credential = new ManagedIdentityCredential(options: new() { AuthorityHost = authorityUri });
                case CredentialType.VisualStudio:
                    return Credential = new VisualStudioCredential(new() { AuthorityHost = authorityUri });
                case CredentialType.VisualStudioCode:
                    return Credential = new VisualStudioCodeCredential(new() { AuthorityHost = authorityUri });
                case CredentialType.AzureCLI:
                    // AzureCLICrediential does not accept options. Azure CLI has built-in cloud profiles so AuthorityHost is not needed.
                    return Credential = new AzureCliCredential();
                case CredentialType.AzurePowerShell:
                    return Credential = new AzurePowerShellCredential(new() { AuthorityHost = authorityUri });
                default:
                    throw new NotImplementedException($"Unexpected credential type '{credentialType}'.");
            }
        }
    }
}