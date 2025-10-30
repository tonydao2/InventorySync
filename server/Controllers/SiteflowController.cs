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

        [HttpPost("test")]
        public IActionResult TestEndpoint([FromBody] List<CSVData> items)
        {

            foreach (var item in items)
            {
                Console.WriteLine($"SKU: {item.Sku}, Quantity: {item.Quantity}");
            }

            // Or inspect it in the debugger
            return Ok(new { message = "Siteflow endpoint is working", count = items.Count});
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
        public async Task<IActionResult> SyncData(List<CSVData> data)
        {
            if (data == null || !data.Any())
                return BadRequest("No data received.");

            var successSkus = new List<string>();
            var failedSkus = new List<string>();
            var errorDetails = new Dictionary<string, string>(); // Store specific error messages per SKU


            foreach (var item in data)
            {
                try
                {
                    Console.WriteLine($"Processing data for SKU: {item.Sku}, Quantity: {item.Quantity}");
                    var result = await _siteflowService.SyncData(item);

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
                Total = data.Count,
                Successful = successSkus.Count,
                Failed = failedSkus.Count,
                SuccessSkus = successSkus,
                FailedSkus = failedSkus,
                ErrorDetails = errorDetails
            });
        }
    }
}
