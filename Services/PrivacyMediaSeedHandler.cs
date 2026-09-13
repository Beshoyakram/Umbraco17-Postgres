using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Centrocdx.Services;

/// <summary>
/// Copies the privacy policy banner from the theme into /media/privacy/ so backoffice Media resolves.
/// </summary>
public class PrivacyMediaSeedHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static int _ran;
    private static readonly Guid PrivacyBannerKey = Guid.Parse("a7010007-2222-4222-8222-211111111701");

    private readonly IMediaService _mediaService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PrivacyMediaSeedHandler> _logger;

    public PrivacyMediaSeedHandler(
        IMediaService mediaService,
        IWebHostEnvironment environment,
        ILogger<PrivacyMediaSeedHandler> logger)
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
            var themeFile = Path.Combine(_environment.WebRootPath, "theme", "centrotheme", "img", "terms-and-condition.png");
            if (!File.Exists(themeFile))
            {
                return Task.CompletedTask;
            }

            var destDir = Path.Combine(_environment.WebRootPath, "media", "privacy");
            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, "banner.png");
            if (!File.Exists(destPath))
            {
                File.Copy(themeFile, destPath, overwrite: false);
            }

            var media = _mediaService.GetById(PrivacyBannerKey);
            if (media == null)
            {
                return Task.CompletedTask;
            }

            const string webPath = "/media/privacy/banner.png";
            var json = $"{{\"src\":\"{webPath}\"}}";
            var current = media.GetValue<string>(Constants.Conventions.Media.File);
            if (!string.Equals(current, json, StringComparison.Ordinal)
                && !string.Equals(current, webPath, StringComparison.Ordinal))
            {
                media.SetValue(Constants.Conventions.Media.File, json);
                _mediaService.Save(media);
                _logger.LogInformation("Privacy banner media path repaired.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Privacy media seed skipped.");
        }

        return Task.CompletedTask;
    }
}
