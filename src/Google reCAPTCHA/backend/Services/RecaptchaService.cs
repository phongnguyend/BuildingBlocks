using ContactForm.Api.Models;
using System.Text.Json;

namespace ContactForm.Api.Services;

public interface IRecaptchaService
{
    Task<bool> VerifyAsync(string token);
}

public class RecaptchaService : IRecaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    private readonly HttpClient _httpClient;
    private readonly string _secretKey;

    public RecaptchaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secretKey = configuration["Recaptcha:SecretKey"]
            ?? throw new InvalidOperationException("Recaptcha:SecretKey is not configured.");
    }

    public async Task<bool> VerifyAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parameters = new Dictionary<string, string>
        {
            ["secret"] = _secretKey,
            ["response"] = token
        };

        using var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync(VerifyUrl, content);

        if (!response.IsSuccessStatusCode)
            return false;

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json);

        return result?.Success == true;
    }
}
