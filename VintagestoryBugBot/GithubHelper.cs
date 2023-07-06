using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace VintagestoryBugBot;

public static class GitHubHelper
{
    private static GitHubClient? _githubClient;
    private static readonly ProductHeaderValue ClientHeader = new("crank-regression-bot");
    private static Credentials? _credentials;

    private static readonly TimeSpan GitHubJwtTimeout = TimeSpan.FromMinutes(5);
    private static Timer _timer;

    private static RsaSecurityKey GetRsaSecurityKeyFromPemKey(string keyText)
    {
        using var rsa = RSA.Create();

        var keyBytes = Convert.FromBase64String(keyText);

        rsa.ImportRSAPrivateKey(keyBytes, out _);

        return new RsaSecurityKey(rsa.ExportParameters(true));
    }

    public static async Task GetCredentialsForAppAsync()
    {
        var credentials =
            new SigningCredentials(GetRsaSecurityKeyFromPemKey(VintagestoryBugBot.Config.GH_APP_KEY),
                SecurityAlgorithms.RsaSha256);

        var jwtToken = new JwtSecurityToken(
            new JwtHeader(credentials),
            new JwtPayload(
                issuer: VintagestoryBugBot.Config.GH_APP_ID,
                issuedAt: DateTime.Now,
                expires: DateTime.Now.Add(GitHubJwtTimeout),
                audience: null,
                claims: null,
                notBefore: null));

        var jwtTokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        var initClient = new GitHubClient(ClientHeader)
        {
            Credentials = new Credentials(jwtTokenString, AuthenticationType.Bearer)
        };

        var installationToken =
            await initClient.GitHubApps.CreateInstallationToken(VintagestoryBugBot.Config.GH_INSTALL_ID);
        _credentials = new Credentials(installationToken.Token, AuthenticationType.Bearer);

        var renewAt = installationToken.ExpiresAt - DateTimeOffset.Now - TimeSpan.FromMinutes(15);
        _timer = new Timer(RenewInstallationToken, null, renewAt, Timeout.InfiniteTimeSpan);

        _githubClient = new GitHubClient(ClientHeader)
        {
            Credentials = _credentials
        };
    }

    private static async void RenewInstallationToken(object? state)
    {
        Console.WriteLine("Renewing API access Token");
        await GetCredentialsForAppAsync();
    }

    public static GitHubClient GetClient() =>
        _githubClient ??= new GitHubClient(ClientHeader)
        {
            Credentials = _credentials
        };
}