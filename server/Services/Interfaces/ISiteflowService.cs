using InventorySync.Models;

namespace InventorySync.Services.Interfaces
{
    public interface ISiteflowSerivce : ISyncService
    {
        Task<SiteflowDataRaw?> GetSiteflowProducts();

        Task<SiteflowDataRaw?> GetSiteflowStockProduct(CSVData data);
    }
}
