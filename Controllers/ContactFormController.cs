using System.ComponentModel.DataAnnotations;
using Centrocdx.Services;
using Microsoft.AspNetCore.Mvc;

namespace Centrocdx.Controllers;

public class ContactFormController : Controller
{
    private readonly IContactSubmissionService _contactSubmissionService;
    private readonly ILogger<ContactFormController> _logger;

    public ContactFormController(
        IContactSubmissionService contactSubmissionService,
        ILogger<ContactFormController> logger)
    {
        _contactSubmissionService = contactSubmissionService;
        _logger = logger;
    }

    [HttpPost("/contact/submit")]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Submit([FromForm] ContactFormModel model, CancellationToken cancellationToken)
    {
        var displayName = BuildDisplayName(model);
        if (string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(model.Email)
            || string.IsNullOrWhiteSpace(model.Phone)
            || string.IsNullOrWhiteSpace(model.Message))
        {
            return Json(new { success = false, message = "Please fill in all required fields correctly." });
        }

        if (!new EmailAddressAttribute().IsValid(model.Email))
        {
            return Json(new { success = false, message = "Please fill in all required fields correctly." });
        }

        try
        {
            var result = await _contactSubmissionService.SaveAsync(
                new ContactSubmissionRequest(
                    displayName,
                    model.Email.Trim(),
                    model.Phone.Trim(),
                    model.Message.Trim(),
                    model.FormKey,
                    model.Occupation,
                    model.Company,
                    model.SourcePage),
                cancellationToken);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage ?? "Could not send your message." });
            }

            return Json(new { success = true, message = "Thank you! Your message has been sent successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form submission failed.");
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Json(new { success = false, message = "Something went wrong. Please try again later." });
        }
    }

    private static string BuildDisplayName(ContactFormModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            return model.Name.Trim();
        }

        var first = model.FirstName?.Trim() ?? string.Empty;
        var last = model.LastName?.Trim() ?? string.Empty;
        return $"{first} {last}".Trim();
    }

    public class ContactFormModel
    {
        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(255)]
        public string? FirstName { get; set; }

        [StringLength(255)]
        public string? LastName { get; set; }

        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Occupation { get; set; }

        [StringLength(255)]
        public string? Company { get; set; }

        [StringLength(100)]
        public string? FormKey { get; set; }

        [StringLength(100)]
        public string? SourcePage { get; set; }
    }
}
