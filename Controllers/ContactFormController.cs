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
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Please fill in all required fields correctly." });
        }

        try
        {
            var result = await _contactSubmissionService.SaveAsync(
                new ContactSubmissionRequest(model.Name, model.Email, model.Phone, model.Message),
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

    public class ContactFormModel
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
