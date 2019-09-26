using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

public static class TestCert
{
    public static X509Certificate2 Load()
    {
        var cert = Path.Combine(System.AppContext.BaseDirectory, "idsrvtest.pfx");
        return new X509Certificate2(cert, "idsrv3test");
    }
    public static SigningCredentials LoadSigningCredentials()
    {
        var cert = Load();
        return new SigningCredentials(new X509SecurityKey(cert), "RS256");
    }
}