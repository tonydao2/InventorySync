namespace InventorySync.Models
{
    public class SyncRequest
    {
        public string Target { get; set; } = ""; // "Moderna", "Syndax"
        public List<CSVData> Data { get; set; } = new List<CSVData>();
    }
}
