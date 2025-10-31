using InventorySync.Models;
using InventorySync.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventorySync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteflowController : ControllerBase
    {
        private readonly ISiteflowSerivce _siteflowService;
        private readonly ILogger _logger;

        public SiteflowController(ISiteflowSerivce siteflowService, ILogger<SiteflowController> logger)
        {
            _siteflowService = siteflowService;
            _logger = logger;
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestEndpoint()
        {
            try
            {
                // Call the service to fetch all Siteflow products
                var products = await _siteflowService.GetSiteflowProducts();

                // Return count and optionally a few items for inspection
                return Ok(new
                {
                    message = "Siteflow endpoint is working",
                    prodcut = products
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching products", error = ex.Message });
            }
        }

        [HttpGet("stock")]
        public async Task<IActionResult> GetStockBySku([FromQuery] CSVData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Sku))
            {
                return BadRequest("Invalid SKU data.");
            }

            var stockItem = await _siteflowService.GetSiteflowStockProduct(data);

            if (stockItem == null)
                return StatusCode(500, "Error fetching item");

            return Ok(stockItem);
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncData([FromBody] SyncRequest request)
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
                    Console.WriteLine($"Processing data for SKU: {item.Sku}, Quantity: {item.Quantity}");
                    var result = await _siteflowService.SyncData(item, request.Target);

                    if (result)
                    {
                        successSkus.Add(item.Sku);
                        _logger.LogInformation("Successful sync for item: {Sku}", item.Sku);
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
