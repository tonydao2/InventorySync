using System.Text.Json.Serialization;

namespace InventorySync.Models
{
    public class SyncRequest
    {
        [JsonPropertyName("targetSite")]
        public string Target { get; set; } = ""; // "Moderna", "Syndax"
        [JsonPropertyName("data")]
        public List<CSVData> Data { get; set; } = new List<CSVData>();
    }
}
