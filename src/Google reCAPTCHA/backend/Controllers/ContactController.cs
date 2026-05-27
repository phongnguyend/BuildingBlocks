using ContactForm.Api.Models;
using ContactForm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContactForm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IRecaptchaService _recaptchaService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IRecaptchaService recaptchaService, ILogger<ContactController> logger)
    {
        _recaptchaService = recaptchaService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ContactFormRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var captchaPassed = await _recaptchaService.VerifyAsync(request.RecaptchaToken);
        if (!captchaPassed)
            return BadRequest(new { error = "reCAPTCHA verification failed. Please try again." });

        _logger.LogInformation(
            "Contact form submission from {Name} <{Email}>: {Message}",
            request.Name, request.Email, request.Message);

        return Ok(new { message = "Your message has been received. Thank you!" });
    }
}
