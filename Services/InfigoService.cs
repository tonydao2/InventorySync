using InventorySync.Services.Interfaces;

namespace InventorySync.Services
{
    /// <summary>
    /// This service is the business logic for Infigo integration. It implements methods defined in IInfigoService.
    /// It is called by the InfigoController to handle requests related to Infigo data synchronization.
    /// </summary>
    public class InfigoService : IInfigoService
    {
        public Task<bool> SyncInfigo(Models.InfigoData infigoData)
        {
            // Implementation for syncing Infigo data goes here.
            throw new NotImplementedException();
        }
    }
}
