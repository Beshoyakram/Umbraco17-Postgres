using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Centrocdx.Options;
using Microsoft.Extensions.Options;

namespace Centrocdx.Services;

public interface IFormEmailNotificationService
{
    Task SendAsync(FormEmailNotification notification, CancellationToken cancellationToken = default);
}

public record FormEmailNotification(
    string FormType,
    string Subject,
    IReadOnlyDictionary<string, string?> Fields,
    string? ReplyToEmail = null,
    FormEmailAttachment? Attachment = null);

public record FormEmailAttachment(
    string FileName,
    string ContentType,
    byte[] ContentBytes);

public class FormEmailNotificationService : IFormEmailNotificationService
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    private readonly AzureAdOptions _azureAd;
    private readonly MailOptions _mail;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FormEmailNotificationService> _logger;
    private readonly SemaphoreSlim _templateLock = new(1, 1);
    private string? _cachedTemplate;

    public FormEmailNotificationService(
        IOptions<AzureAdOptions> azureAd,
        IOptions<MailOptions> mail,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        ILogger<FormEmailNotificationService> logger)
    {
        _azureAd = azureAd.Value;
        _mail = mail.Value;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(FormEmailNotification notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_azureAd.TenantId)
            || string.IsNullOrWhiteSpace(_azureAd.ClientId)
            || string.IsNullOrWhiteSpace(_azureAd.ClientSecret)
            || string.IsNullOrWhiteSpace(_mail.Sender)
            || string.IsNullOrWhiteSpace(_mail.NotificationRecipient))
        {
            _logger.LogWarning("Mail notification skipped because AzureAd/Mail settings are incomplete.");
            return;
        }

        try
        {
            var html = await BuildHtmlAsync(notification, cancellationToken);
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            await SendViaGraphAsync(notification, html, accessToken, cancellationToken);
            _logger.LogInformation(
                "Form notification email sent for {FormType} to {Recipient}.",
                notification.FormType,
                _mail.NotificationRecipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send form notification email for {FormType}.", notification.FormType);
        }
    }

    private async Task<string> BuildHtmlAsync(FormEmailNotification notification, CancellationToken cancellationToken)
    {
        var template = await GetTemplateAsync(cancellationToken);
        var rows = new StringBuilder();

        foreach (var field in notification.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Value))
            {
                continue;
            }

            rows.Append("<tr>")
                .Append("<td style=\"padding:10px 12px;border-bottom:1px solid #e8eef2;color:#025376;font-weight:600;width:34%;vertical-align:top;\">")
                .Append(WebUtility.HtmlEncode(field.Key))
                .Append("</td>")
                .Append("<td style=\"padding:10px 12px;border-bottom:1px solid #e8eef2;color:#333;vertical-align:top;white-space:pre-wrap;\">")
                .Append(WebUtility.HtmlEncode(field.Value))
                .Append("</td>")
                .Append("</tr>");
        }

        return template
            .Replace("{{FormType}}", WebUtility.HtmlEncode(notification.FormType), StringComparison.Ordinal)
            .Replace("{{Subject}}", WebUtility.HtmlEncode(notification.Subject), StringComparison.Ordinal)
            .Replace("{{SubmittedAt}}", WebUtility.HtmlEncode(DateTime.Now.ToString("dddd, MMM d, yyyy h:mm tt")), StringComparison.Ordinal)
            .Replace("{{FieldRows}}", rows.ToString(), StringComparison.Ordinal);
    }

    private async Task<string> GetTemplateAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cachedTemplate))
        {
            return _cachedTemplate;
        }

        await _templateLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedTemplate))
            {
                return _cachedTemplate;
            }

            var relativePath = string.IsNullOrWhiteSpace(_mail.TemplatePath)
                ? "email-templates/form-submission.html"
                : _mail.TemplatePath.TrimStart('/', '\\');
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Email template not found at '{fullPath}'.");
            }

            _cachedTemplate = await File.ReadAllTextAsync(fullPath, cancellationToken);
            return _cachedTemplate;
        }
        finally
        {
            _templateLock.Release();
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credential = new ClientSecretCredential(
            _azureAd.TenantId,
            _azureAd.ClientId,
            _azureAd.ClientSecret);

        var token = await credential.GetTokenAsync(
            new TokenRequestContext(GraphScopes),
            cancellationToken);

        return token.Token;
    }

    private async Task SendViaGraphAsync(
        FormEmailNotification notification,
        string html,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var message = new Dictionary<string, object?>
        {
            ["subject"] = notification.Subject,
            ["body"] = new Dictionary<string, object?>
            {
                ["contentType"] = "HTML",
                ["content"] = html
            },
            ["toRecipients"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["emailAddress"] = new Dictionary<string, object?>
                    {
                        ["address"] = _mail.NotificationRecipient
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(notification.ReplyToEmail))
        {
            message["replyTo"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["emailAddress"] = new Dictionary<string, object?>
                    {
                        ["address"] = notification.ReplyToEmail
                    }
                }
            };
        }

        if (notification.Attachment is { ContentBytes.Length: > 0 } attachment)
        {
            message["attachments"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["@odata.type"] = "#microsoft.graph.fileAttachment",
                    ["name"] = attachment.FileName,
                    ["contentType"] = string.IsNullOrWhiteSpace(attachment.ContentType)
                        ? "application/octet-stream"
                        : attachment.ContentType,
                    ["contentBytes"] = Convert.ToBase64String(attachment.ContentBytes)
                }
            };
        }

        var payload = new Dictionary<string, object?>
        {
            ["message"] = message,
            ["saveToSentItems"] = true
        };

        var sender = Uri.EscapeDataString(_mail.Sender);
        var url = $"https://graph.microsoft.com/v1.0/users/{sender}/sendMail";
        var client = _httpClientFactory.CreateClient(nameof(FormEmailNotificationService));

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Graph sendMail failed with {(int)response.StatusCode}: {body}");
        }
    }
}
