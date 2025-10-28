using InventorySync.Models;

namespace InventorySync.Services.Interfaces
{
    public interface ISyncService
    {
        Task<bool> SyncData(CSVData data); // Used in both InfigoService and SiteflowService
    }
}
