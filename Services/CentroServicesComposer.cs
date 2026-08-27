using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Seeds Centro media into Umbraco Media with a clear folder structure.
/// Source files live under wwwroot/assets/centro (seed only) — pick them from Media in content.
/// </summary>
public class CentroMediaSeedNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string VideoMediaTypeAlias = "umbracoMediaVideo";

    private readonly IMediaService _mediaService;
    private readonly IWebHostEnvironment _environment;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGenerators;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly ILogger<CentroMediaSeedNotificationHandler> _logger;

    public CentroMediaSeedNotificationHandler(
        IMediaService mediaService,
        IWebHostEnvironment environment,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        ILogger<CentroMediaSeedNotificationHandler> logger)
    {
        _mediaService = mediaService;
        _environment = environment;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGenerators = mediaUrlGenerators;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            Seed();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Centro media seed skipped or failed. Upload media manually under Media / Centro.");
        }

        return Task.CompletedTask;
    }

    private void Seed()
    {
        var rootAssets = Path.Combine(_environment.WebRootPath, "assets", "centro");
        if (!Directory.Exists(rootAssets))
        {
            return;
        }

        var centroRoot = EnsureFolder("Centro", Constants.System.Root);
        var brand = EnsureFolder("Brand", centroRoot.Id);
        var home = EnsureFolder("Home", centroRoot.Id);
        var partners = EnsureFolder("Partners", centroRoot.Id);
        var solutions = EnsureFolder("Solutions", centroRoot.Id);

        UploadIfMissing(brand.Id, "Logo", Path.Combine(rootAssets, "logo.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(brand.Id, "Footer Logo", Path.Combine(rootAssets, "footer-logo.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(brand.Id, "Favicon", Path.Combine(rootAssets, "icon.jpg"), Constants.Conventions.MediaTypes.Image);

        UploadIfMissing(home.Id, "About", Path.Combine(rootAssets, "about.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(home.Id, "Hero Fallback", Path.Combine(rootAssets, "banner", "homebg.jpeg"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(home.Id, "AI Background", Path.Combine(rootAssets, "banner", "ai-bg.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(home.Id, "Testimonials Background", Path.Combine(rootAssets, "banner", "testimonial-bg.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(home.Id, "Footer Background", Path.Combine(rootAssets, "banner", "footer-bg.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(home.Id, "Contact", Path.Combine(rootAssets, "consultation.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(home.Id, "Hero Video", Path.Combine(rootAssets, "banner", "hero.mp4"), VideoMediaTypeAlias);

        UploadIfMissing(solutions.Id, "BPO", Path.Combine(rootAssets, "solutions", "bpo.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(solutions.Id, "Healthcare", Path.Combine(rootAssets, "solutions", "healthcare.png"), Constants.Conventions.MediaTypes.Image);
        UploadIfMissing(solutions.Id, "Digital Transformation", Path.Combine(rootAssets, "solutions", "digital.png"), Constants.Conventions.MediaTypes.Image);

        var partnersDir = Path.Combine(rootAssets, "partners");
        if (Directory.Exists(partnersDir))
        {
            foreach (var partnerFile in Directory.GetFiles(partnersDir, "*.png"))
            {
                var name = "Partner " + Path.GetFileNameWithoutExtension(partnerFile);
                UploadIfMissing(partners.Id, name, partnerFile, Constants.Conventions.MediaTypes.Image);
            }
        }

        _logger.LogInformation("Centro media ready under Media / Centro (Brand, Home, Partners, Solutions).");
    }

    private IMedia EnsureFolder(string name, int parentId)
    {
        var existing = _mediaService.GetPagedChildren(parentId, 0, 200, out _)
            .FirstOrDefault(x => x.ContentType.Alias == Constants.Conventions.MediaTypes.Folder
                                 && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return existing;
        }

        var folder = _mediaService.CreateMedia(name, parentId, Constants.Conventions.MediaTypes.Folder);
        _mediaService.Save(folder);
        return folder;
    }

    private void UploadIfMissing(int parentId, string name, string filePath, string mediaTypeAlias)
    {
        if (!System.IO.File.Exists(filePath))
        {
            return;
        }

        var exists = _mediaService.GetPagedChildren(parentId, 0, 500, out _)
            .Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exists)
        {
            return;
        }

        var media = _mediaService.CreateMedia(name, parentId, mediaTypeAlias);
        using var stream = System.IO.File.OpenRead(filePath);
        media.SetValue(
            _mediaFileManager,
            _mediaUrlGenerators,
            _shortStringHelper,
            _contentTypeBaseServiceProvider,
            Constants.Conventions.Media.File,
            Path.GetFileName(filePath),
            stream);
        _mediaService.Save(media);
    }
}

public class CentroServicesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ISiteInfoService, SiteInfoService>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, CentroMediaSeedNotificationHandler>();
    }
}
