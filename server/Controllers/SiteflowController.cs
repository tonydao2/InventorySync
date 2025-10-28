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

        [HttpGet("stocks")]
        public async Task<IActionResult> GetStock()
        {
            var stock = await _siteflowService.GetSiteflowStockProducts();

            if (stock == null)
                return StatusCode(500, "Error fetching stock from Siteflow API");

            return Ok(stock);
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
        public async Task<IActionResult> SyncData(CSVData data)
        {
            try
            {
                var result = await _siteflowService.SyncData(data);

                if (result)
                {
                    return Ok("Data synchronized successfully.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Data synchronization failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data synchronization.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during data synchronization.");
            }
        }
    }
}
