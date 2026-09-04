using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace Centrocdx.Services;

public interface ICareerApplicationService
{
    Task<CareerApplicationResult> SaveAsync(CareerApplicationRequest request, CancellationToken cancellationToken = default);
}

public record CareerApplicationRequest(
    Guid JobKey,
    string Name,
    string Email,
    string? Phone,
    string? Message,
    IFormFile Cv);

public record CareerApplicationResult(bool Success, string? ErrorMessage = null);

public class CareerApplicationService : ICareerApplicationService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    private static readonly long MaxFileBytes = 5 * 1024 * 1024;

    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGenerators;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ILogger<CareerApplicationService> _logger;

    public CareerApplicationService(
        IContentService contentService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        UmbracoHelper umbracoHelper,
        ILogger<CareerApplicationService> logger)
    {
        _contentService = contentService;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGenerators = mediaUrlGenerators;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _umbracoHelper = umbracoHelper;
        _logger = logger;
    }

    public async Task<CareerApplicationResult> SaveAsync(
        CareerApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Cv == null || request.Cv.Length == 0)
        {
            return new CareerApplicationResult(false, "Please upload your CV (doc, docx, or pdf).");
        }

        if (request.Cv.Length > MaxFileBytes)
        {
            return new CareerApplicationResult(false, "CV must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(request.Cv.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return new CareerApplicationResult(false, "CV must be a doc, docx, or pdf file.");
        }

        var job = _contentService.GetById(request.JobKey);
        if (job == null || !string.Equals(job.ContentType.Alias, "careerDetailPage", StringComparison.OrdinalIgnoreCase))
        {
            return new CareerApplicationResult(false, "This job posting is not available.");
        }

        var publishedJob = _umbracoHelper.Content(request.JobKey);
        if (publishedJob == null || !publishedJob.IsPublished())
        {
            return new CareerApplicationResult(false, "This job posting is not available.");
        }

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var phone = request.Phone?.Trim();
        var message = request.Message?.Trim();

        Guid? mediaKey;
        try
        {
            mediaKey = await SaveCvMediaAsync(request.Cv, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store CV media for job {JobKey}.", request.JobKey);
            return new CareerApplicationResult(false, "Could not upload your CV. Please try again.");
        }

        var nodeName = $"{name} — {DateTime.Now:yyyy-MM-dd HH:mm}";
        var submission = _contentService.Create(nodeName, job.Id, "careerApplication");
        submission.SetValue("applicantName", name);
        submission.SetValue("email", email);
        if (!string.IsNullOrWhiteSpace(phone))
        {
            submission.SetValue("phone", phone);
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            submission.SetValue("message", message);
        }

        if (mediaKey.HasValue)
        {
            var pickerJson = JsonSerializer.Serialize(new[]
            {
                new
                {
                    key = Guid.NewGuid(),
                    mediaKey = mediaKey.Value,
                    mediaTypeAlias = Constants.Conventions.MediaTypes.File,
                    crops = Array.Empty<object>(),
                    focalPoint = (object?)null
                }
            });
            submission.SetValue("cv", pickerJson);
        }

        var saveResult = _contentService.Save(submission);
        if (!saveResult.Success)
        {
            return new CareerApplicationResult(false, "Could not save your application. Please try again.");
        }

        return new CareerApplicationResult(true);
    }

    private async Task<Guid> SaveCvMediaAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var folder = EnsureCvsFolder();
        var safeName = SanitizeFileName(file.FileName);
        var mediaName = Path.GetFileNameWithoutExtension(safeName);
        if (string.IsNullOrWhiteSpace(mediaName))
        {
            mediaName = "CV";
        }

        var media = _mediaService.CreateMedia(mediaName, folder.Id, Constants.Conventions.MediaTypes.File);

        await using var stream = file.OpenReadStream();
        // Copy to a seekable stream — MediaFileManager may need Length/Position.
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        media.SetValue(
            _mediaFileManager,
            _mediaUrlGenerators,
            _shortStringHelper,
            _contentTypeBaseServiceProvider,
            Constants.Conventions.Media.File,
            safeName,
            memory);

        var saveResult = _mediaService.Save(media);
        if (!saveResult.Success)
        {
            throw new InvalidOperationException("MediaService.Save failed for CV upload.");
        }

        return media.Key;
    }

    private IMedia EnsureCvsFolder()
    {
        const string folderName = "Career CVs";
        var existing = _mediaService.GetRootMedia()
            .FirstOrDefault(x =>
                x.ContentType.Alias.Equals(Constants.Conventions.MediaTypes.Folder, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Name, folderName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            return existing;
        }

        var folder = _mediaService.CreateMedia(folderName, Constants.System.Root, Constants.Conventions.MediaTypes.Folder);
        _mediaService.Save(folder);
        return folder;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        name = Regex.Replace(name, @"[^\w\.\-]+", "-");
        return string.IsNullOrWhiteSpace(name) ? "cv.pdf" : name;
    }
}
