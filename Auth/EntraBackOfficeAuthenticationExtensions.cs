using Centrocdx.Options;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core.DependencyInjection;

namespace Centrocdx.Auth;

public static class EntraBackOfficeAuthenticationExtensions
{
    public static IUmbracoBuilder AddEntraBackOfficeAuthentication(this IUmbracoBuilder builder)
    {
        builder.Services.Configure<EntraIdOptions>(builder.Config.GetSection(EntraIdOptions.SectionName));
        builder.Services.ConfigureOptions<EntraBackOfficeExternalLoginProviderOptions>();

        builder.AddBackOfficeExternalLogins(logins =>
        {
            logins.AddBackOfficeLogin(backOfficeAuthenticationBuilder =>
            {
                var schemeName = BackOfficeAuthenticationBuilder.SchemeForBackOffice(
                    EntraBackOfficeExternalLoginProviderOptions.SchemeName);
                ArgumentNullException.ThrowIfNull(schemeName);

                var entra = builder.Config.GetSection(EntraIdOptions.SectionName).Get<EntraIdOptions>()
                            ?? new EntraIdOptions();

                backOfficeAuthenticationBuilder.AddOpenIdConnect(schemeName, options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{entra.TenantId}/v2.0";
                    options.ClientId = entra.ClientId;
                    options.ClientSecret = entra.ClientSecret;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.UsePkce = true;
                    options.CallbackPath = string.IsNullOrWhiteSpace(entra.CallbackPath)
                        ? "/umbraco-entra-signin"
                        : entra.CallbackPath;
                    options.RequireHttpsMetadata = true;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.MapInboundClaims = false;

                    options.Scope.Clear();
                    options.Scope.Add(OpenIdConnectScope.OpenId);
                    options.Scope.Add(OpenIdConnectScope.Profile);
                    options.Scope.Add(OpenIdConnectScope.Email);

                    options.TokenValidationParameters.NameClaimType = "name";
                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidIssuer =
                        $"https://login.microsoftonline.com/{entra.TenantId}/v2.0";

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = context =>
                        {
                            if (!EntraBackOfficeExternalLoginProviderOptions.IsAllowedEmail(
                                    context.Principal,
                                    entra.AllowedEmailDomain))
                            {
                                context.Fail(
                                    $"Only @{entra.AllowedEmailDomain.TrimStart('@')} Microsoft accounts are allowed.");
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
            });
        });

        return builder;
    }
}
