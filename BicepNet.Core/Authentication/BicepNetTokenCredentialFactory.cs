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
        public TokenCredential CreateChain(IEnumerable<CredentialType> credentialPrecedence, Uri authorityUri)
        {
            // Return ExternalCredential (token parameter) instead of CredentialChain if provided
            if (BicepWrapper.ExternalCredential != null)
            {
                return BicepWrapper.ExternalCredential;
            }

            if (!credentialPrecedence.Any())
            {
                throw new ArgumentException("At least one credential type must be provided.");
            }

            var tokenCredentials = credentialPrecedence.Select(credentialType => CreateSingle(credentialType, authorityUri)).ToArray();

            return new ChainedTokenCredential(tokenCredentials);
        }

        public TokenCredential CreateSingle(CredentialType credentialType, Uri authorityUri)
        {
            switch (credentialType)
            {
                case CredentialType.Environment:
                    return new EnvironmentCredential(new() { AuthorityHost = authorityUri });
                case CredentialType.ManagedIdentity:
                    return new ManagedIdentityCredential(options: new() { AuthorityHost = authorityUri });
                case CredentialType.VisualStudio:
                    return new VisualStudioCredential(new() { AuthorityHost = authorityUri });
                case CredentialType.VisualStudioCode:
                    return new VisualStudioCodeCredential(new() { AuthorityHost = authorityUri });
                case CredentialType.AzureCLI:
                    // AzureCLICrediential does not accept options. Azure CLI has built-in cloud profiles so AuthorityHost is not needed.
                    return new AzureCliCredential();
                case CredentialType.AzurePowerShell:
                    return new AzurePowerShellCredential(new() { AuthorityHost = authorityUri });
                default:
                    throw new NotImplementedException($"Unexpected credential type '{credentialType}'.");
            }
        }
    }
}
