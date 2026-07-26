using System.Text.Json.Serialization;

namespace ECommerce.Infrastructure.DTOs;

public class FrankfurterResponse
{
    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();
}