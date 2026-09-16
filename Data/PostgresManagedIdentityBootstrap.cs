using System.Data.Common;

namespace Centrocdx.Data;

/// <summary>
/// Replaces the Umbraco PostgreSQL provider factory so connections use Entra/Managed Identity
/// when <c>umbracoDbDSN</c> has no password.
/// </summary>
public static class PostgresManagedIdentityBootstrap
{
    public const string ProviderName = "Npgsql2";

    public static void Register(WebApplicationBuilder builder)
    {
        var dataSource = PostgresNpgsqlDataSourceFactory.Create(builder.Configuration);
        builder.Services.AddSingleton(dataSource);

        DbProviderFactories.UnregisterFactory(ProviderName);
        DbProviderFactories.RegisterFactory(ProviderName, new DataSourceNpgsqlDbProviderFactory(dataSource));
    }
}
