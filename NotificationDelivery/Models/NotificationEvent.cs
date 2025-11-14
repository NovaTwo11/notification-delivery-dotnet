using System.Text.Json.Serialization;

namespace NotificationDelivery.Models
{
    public class NotificationEvent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("additionalData")]
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}