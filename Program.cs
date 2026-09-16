
using Centrocdx.Auth;
using Centrocdx.Data;
using Centrocdx.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "seo/legacy-redirects.json",
    optional: false,
    reloadOnChange: true);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddEntraBackOfficeAuthentication()
    .Build();

PostgresManagedIdentityBootstrap.Register(builder);

WebApplication app = builder.Build();


await app.BootUmbracoAsync();

app.UseHttpsRedirection();
app.UseMiddleware<LegacyRedirectMiddleware>();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
