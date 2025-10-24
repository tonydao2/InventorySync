using System.ComponentModel.DataAnnotations;

namespace InventorySync.Models
{
    /// <summary>
    /// This is the data received from the CSV file for inventory synchronization.
    /// </summary>
    public class CSVData
    {
        [Required]
        public string Sku { get; set; }

        // TODO: Change this to real name later
        [Required]
        public int Quantity { get; set; }
    }
}
