using InventorySync.Models;

namespace InventorySync.Services.Interfaces
{
    public interface ISiteflowSerivce : ISyncService
    {
        Task<List<SiteflowDataRaw>> GetSiteflowStockProducts(CSVData SKU);
    }
}
