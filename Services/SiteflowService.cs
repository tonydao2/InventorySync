using InventorySync.Models;
using InventorySync.Services.Interfaces;

namespace InventorySync.Services
{
    public class SiteflowService : ISiteflowSerivce
    {
        private readonly HttpClient _httpClient;
        private readonly string? _siteflowhmacHeader;
        private readonly IConfiguration _configuration;
        private readonly string? _baseURL;
        private readonly ILogger _logger;

        public SiteflowService(IConfiguration configuration, IHttpClientFactory httpClient, ILogger<SiteflowService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient.CreateClient();
            _siteflowhmacHeader = _configuration["Siteflow:HmacKey"];
            _baseURL = _configuration["Siteflow:BaseURL"];
            _logger = logger;
        }

        public Task<List<SiteflowDataRaw>> GetSiteflowStockProducts(CSVData SKU)
        {
            throw new NotImplementedException();

            //try
            //{
                

            //} catch (error )
        }

        public Task<bool> SyncData(CSVData item)
        {
            throw new NotImplementedException();
        }
    }
}
