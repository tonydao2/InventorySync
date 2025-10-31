namespace InventorySync.Models
{
    public class InfigoApiResponse
    {
        public Boolean Success { get; set; }
    }
    public class InfigoData
    {
        public string Sku { get; set; }
        public int Quantity { get; set; }
    }
}
