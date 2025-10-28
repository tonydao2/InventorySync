using InventorySync.Models;

namespace InventorySync.Services.Interfaces
{
    public interface ISiteflowSerivce : ISyncService
    {
        Task<SiteflowApiResponse?> GetSiteflowStockProducts();
    }
}
