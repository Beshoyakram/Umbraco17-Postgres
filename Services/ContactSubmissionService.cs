using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common;

namespace Centrocdx.Services;

public interface IContactSubmissionService
{
    Task<ContactSubmissionResult> SaveAsync(ContactSubmissionRequest request, CancellationToken cancellationToken = default);
}

public record ContactSubmissionRequest(
    string Name,
    string Email,
    string Phone,
    string Message,
    string? FormKey = null,
    string? Occupation = null,
    string? Company = null,
    string? SourcePage = null);

public record ContactSubmissionResult(bool Success, string? ErrorMessage = null);

public class ContactSubmissionService : IContactSubmissionService
{
    private static readonly Guid ContactUsPageKey = Guid.Parse("e5010005-1111-4111-8111-111111111501");
    private const string DefaultFormKey = "home";

    private readonly IContentService _contentService;
    private readonly UmbracoHelper _umbracoHelper;

    public ContactSubmissionService(IContentService contentService, UmbracoHelper umbracoHelper)
    {
        _contentService = contentService;
        _umbracoHelper = umbracoHelper;
    }

    public Task<ContactSubmissionResult> SaveAsync(ContactSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var formKey = string.IsNullOrWhiteSpace(request.FormKey) ? DefaultFormKey : request.FormKey.Trim();
        var inbox = FindInboxByFormKey(formKey)
                    ?? (formKey.Equals("getInTouch", StringComparison.OrdinalIgnoreCase)
                        ? null
                        : _contentService.GetById(ContactUsPageKey))
                    ?? FindFirstContactUsInbox();

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

        if (!string.IsNullOrWhiteSpace(request.Occupation))
        {
            submission.SetValue("occupation", request.Occupation.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Company))
        {
            submission.SetValue("company", request.Company.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.SourcePage))
        {
            submission.SetValue("sourcePage", request.SourcePage.Trim());
        }

        var saveResult = _contentService.Save(submission);
        if (!saveResult.Success)
        {
            return Task.FromResult(new ContactSubmissionResult(false, "Could not save your message. Please try again."));
        }

        return Task.FromResult(new ContactSubmissionResult(true));
    }

    private IContent? FindInboxByFormKey(string formKey)
    {
        var published = _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("contactUsPage"))
            .FirstOrDefault(x =>
                string.Equals(x.Value<string>("formKey"), formKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Name, formKey, StringComparison.OrdinalIgnoreCase));

        return published == null ? null : _contentService.GetById(published.Key);
    }

    private IContent? FindFirstContactUsInbox()
    {
        var published = _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("contactUsPage"))
            .FirstOrDefault();

        return published == null ? null : _contentService.GetById(published.Key);
    }
}
