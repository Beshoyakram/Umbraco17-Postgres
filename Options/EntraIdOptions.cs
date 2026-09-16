namespace Centrocdx.Options;

public class EntraIdOptions
{
    public const string SectionName = "EntraId";

    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/umbraco-entra-signin";
    public string AllowedEmailDomain { get; set; } = "centrocdx.com";
}
