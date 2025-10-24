using InventorySync.Models;

namespace InventorySync.Services.Interfaces
{
    public interface IInfigoService
    {
        Task<bool> SyncInfigo(InfigoData infigoData);
    }
}
