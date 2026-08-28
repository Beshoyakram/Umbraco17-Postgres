using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Centrocdx.Services;

/// <summary>
/// Repairs Hero Video published cache after a bad umbracoFile JSON path broke HybridCache / Url().
/// </summary>
public class HeroVideoMediaRepairHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static int _ran;
    private static readonly Guid HeroVideoKey = Guid.Parse("a140c8d5-0b70-4f6e-95ee-7639cfef26b8");

    private readonly IMediaService _mediaService;
    private readonly ILogger<HeroVideoMediaRepairHandler> _logger;

    public HeroVideoMediaRepairHandler(IMediaService mediaService, ILogger<HeroVideoMediaRepairHandler> logger)
    {
        _mediaService = mediaService;
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
            var media = _mediaService.GetById(HeroVideoKey);
            if (media == null)
            {
                return Task.CompletedTask;
            }

            var raw = media.GetValue<string>(Constants.Conventions.Media.File);
            var path = NormalizeMediaPath(raw) ?? "/media/ijrjqhzp/hero.mp4";

            if (!string.Equals(raw, path, StringComparison.Ordinal))
            {
                media.SetValue(Constants.Conventions.Media.File, path);
            }

            // Re-save to rebuild cmsContentNu / HybridCache with a valid video path
            _mediaService.Save(media);
            _logger.LogInformation("Repaired Hero Video media path to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hero Video media repair skipped.");
        }

        return Task.CompletedTask;
    }

    private static string? NormalizeMediaPath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        raw = raw.Trim();
        if (raw.StartsWith('/'))
        {
            return raw;
        }

        if (!raw.StartsWith('{'))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("src", out var src))
            {
                return src.GetString();
            }
        }
        catch (JsonException)
        {
            var start = raw.IndexOf("/media", StringComparison.OrdinalIgnoreCase);
            if (start >= 0)
            {
                var end = raw.IndexOf('"', start);
                if (end > start)
                {
                    return raw[start..end];
                }
            }
        }

        return null;
    }
}

public class CentroServicesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ISiteInfoService, SiteInfoService>();
        builder.Services.AddScoped<IContactSubmissionService, ContactSubmissionService>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, HeroVideoMediaRepairHandler>();
    }
}
