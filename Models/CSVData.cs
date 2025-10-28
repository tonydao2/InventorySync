using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InventorySync.Models
{
    /// <summary>
    /// This is the data received from the CSV file for inventory synchronization.
    /// </summary>
    public class CSVData
    {
        [Required]
        [JsonPropertyName("SKU")]
        public string Sku { get; set; }

        [Required]
        [JsonPropertyName("Available Primary")]
        public int Quantity { get; set; }
    }
}
