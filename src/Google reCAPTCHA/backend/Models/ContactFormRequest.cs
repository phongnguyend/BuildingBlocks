using System.ComponentModel.DataAnnotations;

namespace ContactForm.Api.Models;

public class ContactFormRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required.")]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required(ErrorMessage = "reCAPTCHA token is required.")]
    public string RecaptchaToken { get; set; } = string.Empty;
}
