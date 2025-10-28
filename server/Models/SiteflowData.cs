using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace InventorySync.Models
{
    /// <summary>
    /// Data received from Siteflow api calls for inventory synchronization.
    /// </summary>
    public class SiteflowApiResponse
    {
        public List<SiteflowDataRaw>? Data { get; set; }
        public bool Success { get; set; }
        public int Count { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
    }
    public class SiteflowDataRaw
    {
        [JsonPropertyName("_id")]  // maps JSON "_id" to C# Id
        public string Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
