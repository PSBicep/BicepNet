using System;

namespace BicepNet.Core.Authentication;

public class BicepAccessToken
{
    public string Token { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }

    public BicepAccessToken(string token, DateTimeOffset expiresOn)
    {
        Token = token;
        ExpiresOn = expiresOn;
    }

    public override string ToString()
    {
        return Token;
    }
}
