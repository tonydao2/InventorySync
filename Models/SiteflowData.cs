using System.ComponentModel.DataAnnotations;

namespace InventorySync.Models
{
    /// <summary>
    /// Data received from Siteflow api calls for inventory synchronization.
    /// </summary>
    public class SiteflowApiResponse
    {
        public List<SiteflowDataRaw> Data { get; set; }
        public bool Success { get; set; }
        public int Count { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
    }
    public class SiteflowDataRaw
    {
        public string _id { get; set; }
        public string code { get; set; }
        public string barcode { get; set; }
        public string name { get; set; }
    }
}
