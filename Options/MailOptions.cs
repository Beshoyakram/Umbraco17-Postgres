namespace Centrocdx.Options;

public class MailOptions
{
    public const string SectionName = "Mail";

    public string Sender { get; set; } = string.Empty;
    public string NotificationRecipient { get; set; } = string.Empty;
    public string TemplatePath { get; set; } = "email-templates/form-submission.html";
}
