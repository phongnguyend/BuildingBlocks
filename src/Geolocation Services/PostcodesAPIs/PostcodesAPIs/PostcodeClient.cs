using System.Text.Json;

namespace PostcodesAPIs;

public class PostcodeClient
{
    private readonly HttpClient _httpClient;

    public PostcodeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// http://postcodes.io/docs/postcode/nearest
    /// </summary>
    /// <param name="targetPostcode"></param>
    /// <param name="radiusInMeters"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    public async Task<List<PostcodeResult>> FindNearestPostcodesAsync(string targetPostcode, int radiusInMeters = 100, int limit = 10)
    {
        // Clean up user input (e.g., "NW1 9HZ" -> "NW19HZ")
        string cleanPostcode = targetPostcode.Replace(" ", "").ToUpper();

        // Postcodes.io endpoint for finding nearest postcodes around a target
        string url = $"https://api.postcodes.io/postcodes/{cleanPostcode}/nearest?radius={radiusInMeters}&limit={limit}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);

        await EnsureSuccessAsync(response);

        string jsonString = await response.Content.ReadAsStringAsync();

        var apiData = JsonSerializer.Deserialize<ApiResponse>(jsonString);

        return apiData?.Result ?? new List<PostcodeResult>();
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
