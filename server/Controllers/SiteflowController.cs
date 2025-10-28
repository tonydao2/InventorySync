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


            try
            {
                foreach (var item in data)
                {
                    Console.WriteLine($"Processing data for {item.Sku} to change to {item.Quantity}");
                    var result = await _siteflowService.SyncData(item);

                    if (result)
                    {
                        successSkus.Add(item.Sku);
                        _logger.LogInformation($"Successful sync for item: {item.Sku}");
                    }
                    else
                    {
                        failedSkus.Add(item.Sku);
                        _logger.LogWarning($"Error synchronizing SKU: {item.Sku}");
                    }
                }

                return Ok(new
                {
                    Total = data.Count,
                    Successful = successSkus.Count,
                    Failed = failedSkus.Count,
                    SuccessSkus = successSkus,
                    FailedSkus = failedSkus
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data synchronization.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during data synchronization.");
            }
        }
    }
}
