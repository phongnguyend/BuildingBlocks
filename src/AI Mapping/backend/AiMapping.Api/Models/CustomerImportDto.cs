namespace AiMapping.Api.Models;

public sealed class CustomerImportDto
{
    public int SourceRowNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public DateOnly? SignupDate { get; set; }
    public decimal? AnnualRevenue { get; set; }
}
