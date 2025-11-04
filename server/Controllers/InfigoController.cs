using InventorySync.Models;
using InventorySync.Services;
using InventorySync.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventorySync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfigoController : ControllerBase
    {
        private readonly IInfigoService _infigoService;
        private readonly ILogger _logger;

        public InfigoController(ILogger<InfigoController> logger, IInfigoService infigoService)
        {
            _infigoService = infigoService;
            _logger = logger;
        }


        [HttpPost("sync")]
        public async Task<IActionResult> SyncInfigoData([FromBody] SyncRequest request)
        {
            if (request.Data == null || !request.Data.Any())
                return BadRequest("No data provided");

            var successSkus = new List<string>();
            var failedSkus = new List<string>();
            var errorDetails = new Dictionary<string, string>(); // Store specific error messages per SKU

            foreach (var item in request.Data)
            {
                try
                {
                    var result = await _infigoService.SyncData(item, request.Target);

                    if (result)
                    {
                        successSkus.Add(item.Sku);
                    }
                    else
                    {
                        failedSkus.Add(item.Sku);
                        _logger.LogWarning("Failed to sync SKU: {Sku}", item.Sku);
                        errorDetails[item.Sku] = "Sync returned false (possibly item not found or update failed)";
                    }
                }
                catch (Exception ex)
                {
                    failedSkus.Add(item.Sku);
                    _logger.LogError(ex, "Exception syncing SKU: {Sku}", item.Sku);
                    errorDetails[item.Sku] = ex.Message;
                }
            }

            // Return full summary
            return Ok(new
            {
                Total = request.Data.Count,
                Successful = successSkus.Count,
                Failed = failedSkus.Count,
                SuccessSkus = successSkus,
                FailedSkus = failedSkus,
                ErrorDetails = errorDetails
            });
        }
    }
}
