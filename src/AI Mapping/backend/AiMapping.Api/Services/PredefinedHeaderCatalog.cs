using AiMapping.Api.Models;

namespace AiMapping.Api.Services;

public sealed class PredefinedHeaderCatalog
{
    private static readonly IReadOnlyList<PredefinedHeader> Headers = new[]
    {
        new PredefinedHeader("firstName", "First name", "string", new[] { "first", "given name", "forename", "fname" }, true),
        new PredefinedHeader("lastName", "Last name", "string", new[] { "last", "surname", "family name", "lname" }, true),
        new PredefinedHeader("email", "Email", "string", new[] { "email address", "e-mail", "mail" }, true),
        new PredefinedHeader("phoneNumber", "Phone number", "string", new[] { "phone", "mobile", "telephone", "cell" }, false),
        new PredefinedHeader("company", "Company", "string", new[] { "organization", "organisation", "employer", "account" }, false),
        new PredefinedHeader("jobTitle", "Job title", "string", new[] { "title", "role", "position" }, false),
        new PredefinedHeader("country", "Country", "string", new[] { "nation", "market", "region country" }, false),
        new PredefinedHeader("city", "City", "string", new[] { "town", "municipality", "location" }, false),
        new PredefinedHeader("signupDate", "Signup date", "date", new[] { "created date", "registration date", "registered at", "joined", "join date" }, false),
        new PredefinedHeader("annualRevenue", "Annual revenue", "decimal", new[] { "revenue", "arr", "sales", "turnover" }, false)
    };

    public IReadOnlyList<PredefinedHeader> GetHeaders() => Headers;
}
