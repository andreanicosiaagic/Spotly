using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace Spotly.Api.Infrastructure;

/// <summary>
/// Custom SqlAuthenticationProvider per sviluppo locale.
/// Usa AzureCliCredential → VisualStudioCredential con timeout esplicito (15 s)
/// per evitare hang infiniti durante lo startup.
/// </summary>
internal sealed class DevSqlAuthProvider : SqlAuthenticationProvider
{
    private static readonly TokenCredential Credential = new ChainedTokenCredential(
        new AzureCliCredential(),
        new VisualStudioCredential()
    );

    private static readonly TokenRequestContext SqlTokenContext =
        new(["https://database.windows.net/.default"]);

    public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            var token = await Credential.GetTokenAsync(SqlTokenContext, cts.Token);
            return new SqlAuthenticationToken(token.Token, token.ExpiresOn);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Timeout acquisizione token Azure SQL (>15 s). " +
                "Esegui 'az login' oppure imposta Database__Provider=InMemory per sviluppo offline.");
        }
    }

    public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
        => authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryDefault;
}
