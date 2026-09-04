using System.ComponentModel.DataAnnotations;
using Centrocdx.Services;
using Microsoft.AspNetCore.Mvc;

namespace Centrocdx.Controllers;

public class CareerApplicationController : Controller
{
    private readonly ICareerApplicationService _careerApplicationService;
    private readonly ILogger<CareerApplicationController> _logger;

    public CareerApplicationController(
        ICareerApplicationService careerApplicationService,
        ILogger<CareerApplicationController> logger)
    {
        _careerApplicationService = careerApplicationService;
        _logger = logger;
    }

    [HttpPost("/careers/apply")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Produces("application/json")]
    public async Task<IActionResult> Apply([FromForm] CareerApplicationModel model, CancellationToken cancellationToken)
    {
        if (model.JobKey == Guid.Empty
            || string.IsNullOrWhiteSpace(model.Name)
            || string.IsNullOrWhiteSpace(model.Email)
            || model.Cv == null)
        {
            return Json(new { success = false, message = "Please fill in all required fields and upload your CV." });
        }

        if (!new EmailAddressAttribute().IsValid(model.Email))
        {
            return Json(new { success = false, message = "Please enter a valid email address." });
        }

        try
        {
            var result = await _careerApplicationService.SaveAsync(
                new CareerApplicationRequest(
                    model.JobKey,
                    model.Name.Trim(),
                    model.Email.Trim(),
                    model.Phone,
                    model.Message,
                    model.Cv),
                cancellationToken);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage ?? "Could not submit your application." });
            }

            return Json(new { success = true, message = "Thank you! Your application has been submitted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Career application failed for job {JobKey}.", model.JobKey);
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Json(new { success = false, message = "Something went wrong. Please try again later." });
        }
    }

    public class CareerApplicationModel
    {
        public Guid JobKey { get; set; }

        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(2000)]
        public string? Message { get; set; }

        public IFormFile? Cv { get; set; }
    }
}
