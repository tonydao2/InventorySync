using InventorySync.Models;
using InventorySync.Services.Interfaces;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace InventorySync.Services
{
    /// <summary>
    /// This service is the business logic for Infigo integration. It implements methods defined in IInfigoService.
    /// It is called by the InfigoController to handle requests related to Infigo data synchronization.
    /// </summary>
    public class InfigoService : IInfigoService
    {
    private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpclient;
        private readonly ILogger _logger;
        private readonly string? _infigoToken;

        public InfigoService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<InfigoService> logger) {
            _logger = logger;
            _httpclient = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<bool> SyncData(CSVData data, string target)
        {
            _logger.LogInformation("Syncing Infigo data for SKU: {SKU}, Quantity: {Quantity}", data.Sku, data.Quantity);

            var client = _httpclient.CreateClient("infigo");

            var token = _configuration[$"Infigo:{target}:Token"];

            Console.WriteLine($"Infigo Token: " + token);

            client.DefaultRequestHeaders.Add("Authorization", "Basic " + token);

            var body = new
            {
                SKU = data.Sku,
                IncludeAttributeCombination = false,
                RequireExactMatch = true,
                StockValue = data.Quantity,
                IsAbsoluteAdjustment = true
            };

            string jsonContent = JsonSerializer.Serialize(body);
            StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("catalog/product/stockbysku", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned HTTP {StatusCode}", response.StatusCode);
                return false;
            }

            // Get Content from response
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize content
            InfigoApiResponse? infigoResponse = JsonSerializer.Deserialize<InfigoApiResponse>(responseBody);

            Console.WriteLine($"Success: {infigoResponse?.Success}");

            if (infigoResponse?.Success == true)
            {
                return true;
            }

            return false;
        }
    }
}
