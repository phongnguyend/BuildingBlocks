using System.Text.Json.Serialization;

namespace Loqate;

public sealed class LoqateFindResponse
{
    [JsonPropertyName("Items")]
    public List<LoqateFindItem> Items { get; set; } = [];
}

public sealed class LoqateFindItem
{
    public string Id { get; set; } = "";

    public string Type { get; set; } = "";

    public string Text { get; set; } = "";

    public string Description { get; set; } = "";

    public string Highlight { get; set; } = "";
}

public sealed class LoqateRetrieveResponse
{
    [JsonPropertyName("Items")]
    public List<LoqateAddress> Items { get; set; } = [];
}

public sealed class LoqateAddress
{
    public string Id { get; set; } = "";

    public string Company { get; set; } = "";

    public string BuildingNumber { get; set; } = "";

    public string BuildingName { get; set; } = "";

    public string Street { get; set; } = "";

    public string City { get; set; } = "";

    public string Province { get; set; } = "";

    public string PostalCode { get; set; } = "";

    public string CountryName { get; set; } = "";

    public string Line1 { get; set; } = "";

    public string Line2 { get; set; } = "";

    public string Line3 { get; set; } = "";

    public string Line4 { get; set; } = "";

    public string Line5 { get; set; } = "";
}

public sealed class LoqateVerifyRequest
{
    public string Key { get; set; } = string.Empty;

    public bool? Geocode { get; set; }

    public LoqateVerifyOptions? Options { get; set; }

    public List<LoqateVerifyAddress> Addresses { get; set; } = [];
}

public sealed class LoqateVerifyOptions
{
    public string? Process { get; set; } = "Verify";

    public bool? Certify { get; set; }

    public bool? Enhance { get; set; }

    public bool? Version { get; set; }

    public Dictionary<string, object>? ServerOptions { get; set; }
}

public sealed class LoqateVerifyAddress
{
    public string? Address { get; set; }

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }

    public string? Address3 { get; set; }

    public string? Address4 { get; set; }

    public string? Address5 { get; set; }

    public string? Address6 { get; set; }

    public string? Address7 { get; set; }

    public string? Address8 { get; set; }

    public string? Locality { get; set; }

    public string? AdministrativeArea { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }
}

public sealed class LoqateVerifyResult
{
    public LoqateVerifyAddress? Input { get; set; }

    public List<LoqateVerifiedAddress> Matches { get; set; } = [];
}

public sealed class LoqateVerifiedAddress
{
    public string? AVC { get; set; }

    public string? AQI { get; set; }

    public string? Address { get; set; }

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }

    public string? Address3 { get; set; }

    public string? Address4 { get; set; }

    public string? Address5 { get; set; }

    public string? Address6 { get; set; }

    public string? Address7 { get; set; }

    public string? Address8 { get; set; }

    public string? Locality { get; set; }

    public string? AdministrativeArea { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? CountryName { get; set; }

    public string? Latitude { get; set; }

    public string? Longitude { get; set; }
}
