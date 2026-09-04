using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Centrocdx.Services;

/// <summary>
/// Copies the careers banner from the theme into /media/careers/ so backoffice Media resolves.
/// </summary>
public class CareerMediaSeedHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static int _ran;
    private static readonly Guid CareersBannerKey = Guid.Parse("a7010005-2222-4222-8222-211111111701");

    private readonly IMediaService _mediaService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CareerMediaSeedHandler> _logger;

    public CareerMediaSeedHandler(
        IMediaService mediaService,
        IWebHostEnvironment environment,
        ILogger<CareerMediaSeedHandler> logger)
    {
        _mediaService = mediaService;
        _environment = environment;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _ran, 1) == 1)
        {
            return Task.CompletedTask;
        }

        try
        {
            var themeFile = Path.Combine(_environment.WebRootPath, "theme", "centrotheme", "img", "Career 2.png");
            if (!File.Exists(themeFile))
            {
                return Task.CompletedTask;
            }

            var destDir = Path.Combine(_environment.WebRootPath, "media", "careers");
            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, "banner.png");
            if (!File.Exists(destPath))
            {
                File.Copy(themeFile, destPath, overwrite: false);
            }

            var media = _mediaService.GetById(CareersBannerKey);
            if (media == null)
            {
                return Task.CompletedTask;
            }

            const string webPath = "/media/careers/banner.png";
            var json = $"{{\"src\":\"{webPath}\"}}";
            var current = media.GetValue<string>(Constants.Conventions.Media.File);
            if (!string.Equals(current, json, StringComparison.Ordinal)
                && !string.Equals(current, webPath, StringComparison.Ordinal))
            {
                media.SetValue(Constants.Conventions.Media.File, json);
                _mediaService.Save(media);
                _logger.LogInformation("Careers banner media path repaired.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Careers media seed skipped.");
        }

        return Task.CompletedTask;
    }
}
