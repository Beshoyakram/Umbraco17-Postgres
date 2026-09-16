using Azure.Core;
using Azure.Identity;
using Npgsql;

namespace Centrocdx.Data;

/// <summary>
/// Builds <see cref="NpgsqlDataSource"/> with optional Entra ID when the connection string has no password.
/// User Entra auth (email username): use <c>az login</c> locally; token comes from DefaultAzureCredential.
/// Managed identity: set PostgreSql:UseManagedIdentity=true and PostgreSql:ManagedIdentityClientId.
/// </summary>
public static class PostgresNpgsqlDataSourceFactory
{
    private static readonly string[] PostgresEntraScopes = { "https://ossrdbms-aad.database.windows.net/.default" };

    public static NpgsqlDataSource Create(IConfiguration configuration, string connectionName = "umbracoDbDSN")
    {
        var connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' was not found.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        if (dataSourceBuilder.ConnectionStringBuilder.Password is null)
        {
            ConfigureEntraAuth(dataSourceBuilder, configuration);
        }

        return dataSourceBuilder.Build();
    }

    public static void ConfigureEntraAuth(NpgsqlDataSourceBuilder dataSourceBuilder, IConfiguration configuration)
    {
        var credential = CreateCredential(configuration);
        dataSourceBuilder.UsePeriodicPasswordProvider(
            async (_, cancellationToken) =>
            {
                var token = await credential.GetTokenAsync(new TokenRequestContext(PostgresEntraScopes), cancellationToken);
                return token.Token;
            },
            successRefreshInterval: TimeSpan.FromHours(4),
            failureRefreshInterval: TimeSpan.FromSeconds(10));
    }

    private static TokenCredential CreateCredential(IConfiguration configuration)
    {
        var useManagedIdentity = configuration.GetValue("PostgreSql:UseManagedIdentity", false);
        var managedIdentityClientId = configuration["PostgreSql:ManagedIdentityClientId"];

        if (useManagedIdentity && !string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });
        }

        return new DefaultAzureCredential();
    }
}
