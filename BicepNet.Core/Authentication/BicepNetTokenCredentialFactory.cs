using Azure.Core;
using Azure.Identity;
using Bicep.Core.Configuration;
using Bicep.Core.Registry.Auth;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace BicepNet.Core.Authentication;

public class BicepNetTokenCredentialFactory : ITokenCredentialFactory
{
    public static string Scope { get; } = "https://management.core.windows.net/.default";
    
    internal ILogger? logger { get; set; }
    internal TokenRequestContext TokenRequestContext { get; set; }
    internal TokenCredential? Credential { get; set; }
    internal bool InteractiveAuthentication { get; set; }

    public TokenCredential CreateChain(IEnumerable<CredentialType> credentialPrecedence, Uri authorityUri)
    {
        // Return the credential if already authenticated in BicepNet
        if (Credential is not null)
        {
            return Credential;
        }

        // If not authenticated, ensure BicepConfig has a precedence
        if (!credentialPrecedence.Any())
        {
            throw new ArgumentException($"At least one credential type must be provided.");
        }

        // Authenticate using BicepConfig precedence
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

    internal void Clear()
    {
        InteractiveAuthentication = false;

        if (Credential == null)
        {
            logger?.LogInformation("No stored credential to clear.");
            return;
        }

        Credential = null;
        logger?.LogInformation("Cleared stored credential.");
    }

    internal void SetToken(Uri activeDirectoryAuthorityUri, string? token = null, string? tenantId = null)
    {
        // User provided a token
        if (!string.IsNullOrWhiteSpace(token))
        {
            logger?.LogInformation("Token provided as authentication.");
            InteractiveAuthentication = false;

            // Try to parse JWT for expiry date
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token);
                var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals("exp")).Value;
                var expDateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(tokenExp));

                logger?.LogInformation("Successfully parsed token, expiration date is {expDateTime}.", expDateTime);
                Credential = new ExternalTokenCredential(token, expDateTime);
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
            InteractiveAuthentication = true;
            Credential = new InteractiveBrowserCredential(options: new() { AuthorityHost = activeDirectoryAuthorityUri });
            TokenRequestContext = new TokenRequestContext(new[] { Scope }, tenantId: tenantId);

            // Get token immediately to trigger browser prompt, instead of waiting until the credential is used
            // The token is then stored in the Credential object, here we don't care about the return value
            GetToken();

            logger?.LogInformation("Authentication successful.");
        }
    }

    public AccessToken? GetToken()
    {
        return Credential?.GetToken(TokenRequestContext, System.Threading.CancellationToken.None);
    }
}