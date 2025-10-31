using InventorySync.Models;

namespace InventorySync.Services.Interfaces
{
    public interface ISyncService
    {
        Task<bool> SyncData(CSVData data, string targetSite); // Used in both InfigoService and SiteflowService
    }
}
