using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContactAPIs;

public class ApolloContactProvider : IContactProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ApolloContactProvider(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    public async Task<List<ContactResult>> SearchContactsAsync(ContactSearchQuery query)
    {
        // Note: Apollo allows parameters via POST body or URL. Ensure API schema compliance.
        var requestUrl = "https://api.apollo.io/api/v1/mixed_people/api_search";

        var requestBody = new
        {
            q_keywords = query.Keywords,
            person_titles = query.Titles,
            person_locations = query.Locations
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Set Apollo Authentication Header
        request.Headers.Add("Cache-Control", "no-cache");
        request.Headers.Add("X-Api-Key", _apiKey);

        var response = await _httpClient.SendAsync(request);

        await EnsureSuccessAsync(response);

        var responseJson = await response.Content.ReadAsStringAsync();
        var apolloData = JsonSerializer.Deserialize<ApolloSearchResponse>(responseJson);

        var results = new List<ContactResult>();
        if (apolloData?.People != null)
        {
            foreach (var person in apolloData.People)
            {
                results.Add(new ContactResult
                {
                    FirstName = person.FirstName,
                    LastName = person.LastName,
                    Title = person.Title,
                    CompanyName = person.Organization?.Name,
                    Email = person.HasEmail ? "Available (Requires enrichment call)" : null,
                    SourceProvider = "Apollo"
                });
            }
        }

        return results;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}): {body}", null, response.StatusCode);
        }
    }
}

// Internal DTOs to deserialize Apollo specific JSON structure
internal class ApolloSearchResponse
{
    [JsonPropertyName("people")]
    public List<ApolloPerson> People { get; set; }
}

internal class ApolloPerson
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string LastName { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("has_email")]
    public bool HasEmail { get; set; }

    [JsonPropertyName("organization")]
    public ApolloOrganization Organization { get; set; }
}

internal class ApolloOrganization
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}