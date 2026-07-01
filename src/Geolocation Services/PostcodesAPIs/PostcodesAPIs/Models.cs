using System.Text.Json.Serialization;

namespace PostcodesAPIs;

public class ApiResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("result")]
    public List<PostcodeResult> Result { get; set; }
}

public class PostcodeResult
{
    [JsonPropertyName("postcode")]
    public string Postcode { get; set; }

    [JsonPropertyName("distance")]
    public double DistanceInMetres { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
