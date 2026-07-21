using System.Net.Http.Json;

namespace Loqate;

public sealed class LoqateClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public LoqateClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<IReadOnlyList<LoqateFindItem>> FindAsync(
        string searchText,
        string? container = null,
        string? countries = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(
            "Capture/Interactive/Find/v1.20/json6.ws",
            new Dictionary<string, string?>
            {
                ["Key"] = _apiKey,
                ["Text"] = searchText,
                ["Container"] = container,
                ["Countries"] = countries,
                ["Limit"] = limit.ToString()
            });

        var response = await _httpClient.GetFromJsonAsync<LoqateFindResponse>(
            url,
            cancellationToken);

        return response?.Items ?? [];
    }

    public async Task<LoqateAddress?> RetrieveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(
            "Capture/Interactive/Retrieve/v1.30/json6.ws",
            new Dictionary<string, string?>
            {
                ["Key"] = _apiKey,
                ["Id"] = id
            });

        var response = await _httpClient.GetFromJsonAsync<LoqateRetrieveResponse>(
            url,
            cancellationToken);

        return response?.Items.FirstOrDefault();
    }

    private static string BuildUrl(string path, IDictionary<string, string?> parameters)
    {
        var query = string.Join("&",
            parameters
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .Select(p =>
                    $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}"));

        return $"{path}?{query}";
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}): {body}", null, response.StatusCode);
        }
    }

    public async Task<LoqateVerifiedAddress?> VerifyAsync(
    LoqateVerifyAddress address,
    bool geocode = false,
    bool certify = false,
    bool enhance = false,
    CancellationToken cancellationToken = default)
    {
        var request = new LoqateVerifyRequest
        {
            Key = _apiKey,
            Geocode = geocode,
            Options = new LoqateVerifyOptions
            {
                Process = "Verify",
                Certify = certify,
                Enhance = enhance
            },
            Addresses = [address]
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "Cleansing/International/Batch/v1.20/json6.ws",
            request,
            cancellationToken);

        await EnsureSuccessAsync(response);

        var test = await response.Content.ReadAsStringAsync();

        var results = await response.Content.ReadFromJsonAsync<List<LoqateVerifyResult>>(
            cancellationToken: cancellationToken);

        return results?
            .FirstOrDefault()?
            .Matches
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<LoqateVerifyResult>> VerifyAsync(
    IEnumerable<LoqateVerifyAddress> addresses,
    CancellationToken cancellationToken = default)
    {
        var request = new LoqateVerifyRequest
        {
            Key = _apiKey,
            Addresses = addresses.ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "Cleansing/International/Batch/v1.00/json4.ws",
            request,
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<List<LoqateVerifyResult>>(
                   cancellationToken: cancellationToken)
               ?? [];
    }
}
