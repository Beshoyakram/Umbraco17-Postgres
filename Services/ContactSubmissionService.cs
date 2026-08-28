using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common;

namespace Centrocdx.Services;

public interface IContactSubmissionService
{
    Task<ContactSubmissionResult> SaveAsync(ContactSubmissionRequest request, CancellationToken cancellationToken = default);
}

public record ContactSubmissionRequest(string Name, string Email, string Phone, string Message);

public record ContactSubmissionResult(bool Success, string? ErrorMessage = null);

public class ContactSubmissionService : IContactSubmissionService
{
    private static readonly Guid ContactUsPageKey = Guid.Parse("e5010005-1111-4111-8111-111111111501");

    private readonly IContentService _contentService;
    private readonly UmbracoHelper _umbracoHelper;

    public ContactSubmissionService(IContentService contentService, UmbracoHelper umbracoHelper)
    {
        _contentService = contentService;
        _umbracoHelper = umbracoHelper;
    }

    public Task<ContactSubmissionResult> SaveAsync(ContactSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var inbox = _contentService.GetById(ContactUsPageKey)
                    ?? FindContactUsInbox();

        if (inbox == null)
        {
            return Task.FromResult(new ContactSubmissionResult(false, "Contact inbox is not configured."));
        }

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var phone = request.Phone.Trim();
        var message = request.Message.Trim();

        var nodeName = $"{name} — {DateTime.Now:yyyy-MM-dd HH:mm}";
        var submission = _contentService.Create(nodeName, inbox.Id, "contactUsSubmission");

        submission.SetValue("submitterName", name);
        submission.SetValue("email", email);
        submission.SetValue("phone", phone);
        submission.SetValue("message", message);

        var saveResult = _contentService.Save(submission);
        if (!saveResult.Success)
        {
            return Task.FromResult(new ContactSubmissionResult(false, "Could not save your message. Please try again."));
        }

        return Task.FromResult(new ContactSubmissionResult(true));
    }

    private IContent? FindContactUsInbox()
    {
        return _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("contactUsPage"))
            .Select(x => _contentService.GetById(x.Key))
            .FirstOrDefault(x => x != null);
    }
}
