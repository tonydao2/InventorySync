using InventorySync.Models;

namespace InventorySync.Services.Interfaces
{
    public interface ISiteflowSerivce : ISyncService
    {
        Task<List<SiteflowDataRaw>> GetAllSiteflowProducts(string targetSite);

        Task<SiteflowDataRaw?> GetSiteflowStockProduct(CSVData data);
    }
}
