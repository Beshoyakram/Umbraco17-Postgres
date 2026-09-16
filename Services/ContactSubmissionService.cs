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
    private readonly IFormEmailNotificationService _emailNotificationService;

    public ContactSubmissionService(
        IContentService contentService,
        UmbracoHelper umbracoHelper,
        IFormEmailNotificationService emailNotificationService)
    {
        _contentService = contentService;
        _umbracoHelper = umbracoHelper;
        _emailNotificationService = emailNotificationService;
    }

    public async Task<ContactSubmissionResult> SaveAsync(ContactSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var formKey = string.IsNullOrWhiteSpace(request.FormKey) ? DefaultFormKey : request.FormKey.Trim();
        var inbox = FindInboxByFormKey(formKey)
                    ?? (formKey.Equals("getInTouch", StringComparison.OrdinalIgnoreCase)
                        ? null
                        : _contentService.GetById(ContactUsPageKey))
                    ?? FindFirstContactUsInbox();

        if (inbox == null)
        {
            return new ContactSubmissionResult(false, "Contact inbox is not configured.");
        }

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var phone = request.Phone.Trim();
        var message = request.Message.Trim();
        var occupation = request.Occupation?.Trim();
        var company = request.Company?.Trim();
        var sourcePage = request.SourcePage?.Trim();

        var nodeName = $"{name} — {DateTime.Now:yyyy-MM-dd HH:mm}";
        var submission = _contentService.Create(nodeName, inbox.Id, "contactUsSubmission");

        submission.SetValue("submitterName", name);
        submission.SetValue("email", email);
        submission.SetValue("phone", phone);
        submission.SetValue("message", message);

        if (!string.IsNullOrWhiteSpace(occupation))
        {
            submission.SetValue("occupation", occupation);
        }

        if (!string.IsNullOrWhiteSpace(company))
        {
            submission.SetValue("company", company);
        }

        if (!string.IsNullOrWhiteSpace(sourcePage))
        {
            submission.SetValue("sourcePage", sourcePage);
        }

        var saveResult = _contentService.Save(submission);
        if (!saveResult.Success)
        {
            return new ContactSubmissionResult(false, "Could not save your message. Please try again.");
        }

        var fields = new Dictionary<string, string?>
        {
            ["Name"] = name,
            ["Email"] = email,
            ["Phone"] = phone,
            ["Company"] = company,
            ["Source page"] = sourcePage,
            ["Form key"] = formKey,
            ["Message"] = message
        };

        if (!string.IsNullOrWhiteSpace(occupation))
        {
            var subjectOrOccupationLabel = formKey.Equals("getInTouch", StringComparison.OrdinalIgnoreCase)
                ? "Occupation"
                : "Subject";
            fields[subjectOrOccupationLabel] = occupation;
        }

        await _emailNotificationService.SendAsync(
            new FormEmailNotification(
                "Contact form",
                $"New contact form submission from {name}",
                fields,
                email),
            cancellationToken);

        return new ContactSubmissionResult(true);
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
