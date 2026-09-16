using System.Security.Claims;
using Centrocdx.Options;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core;

namespace Centrocdx.Auth;

public class EntraBackOfficeExternalLoginProviderOptions : IConfigureNamedOptions<BackOfficeExternalLoginProviderOptions>
{
    public const string SchemeName = "OpenIdConnect";

    private readonly EntraIdOptions _entraId;

    public EntraBackOfficeExternalLoginProviderOptions(IOptions<EntraIdOptions> entraId)
    {
        _entraId = entraId.Value;
    }

    public void Configure(string? name, BackOfficeExternalLoginProviderOptions options)
    {
        if (name != Constants.Security.BackOfficeExternalAuthenticationTypePrefix + SchemeName)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(BackOfficeExternalLoginProviderOptions options)
    {
        options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
            autoLinkExternalAccount: true,
            defaultUserGroups: ["editor"],
            defaultCulture: null,
            allowManualLinking: true)
        {
            OnAutoLinking = (autoLinkUser, loginInfo) =>
            {
                var displayName = GetDisplayName(loginInfo.Principal);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    autoLinkUser.Name = displayName;
                }
            },
            OnExternalLogin = (user, loginInfo) =>
            {
                if (!IsAllowedEmail(loginInfo.Principal, _entraId.AllowedEmailDomain))
                {
                    return false;
                }

                var displayName = GetDisplayName(loginInfo.Principal);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    user.Name = displayName;
                }

                return true;
            }
        };

        // Keep local username/password login available.
        options.DenyLocalLogin = false;
    }

    internal static bool IsAllowedEmail(ClaimsPrincipal? principal, string allowedDomain)
    {
        if (principal == null || string.IsNullOrWhiteSpace(allowedDomain))
        {
            return false;
        }

        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var domain = allowedDomain.Trim().TrimStart('@');
        return email.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetEmail(ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email)
           ?? principal.FindFirstValue("preferred_username")
           ?? principal.FindFirstValue("email");

    private static string? GetDisplayName(ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return null;
        }

        return principal.FindFirstValue("name")
               ?? principal.FindFirstValue(ClaimTypes.Name)
               ?? GetEmail(principal);
    }
}
