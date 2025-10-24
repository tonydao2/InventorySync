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

        [HttpGet("stock")]
        public async Task<IActionResult> GetStock(CSVData sku)
        {
            var stock = await _siteflowService.GetSiteflowStockProducts(sku);
            return Ok(stock);
        }
        
    }
}
