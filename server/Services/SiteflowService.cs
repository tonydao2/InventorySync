using System.Diagnostics;
using InventorySync.Models;
using InventorySync.Services.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InventorySync.Services
{
    public class SiteflowService : ISiteflowSerivce
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly string? _siteflowSecret;
        private readonly string? _siteflowToken;
        private readonly IConfiguration _configuration;
        private readonly string? _baseURL;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;

        private readonly string cacheKey =  "productsCacheKey";

        public SiteflowService(IConfiguration configuration,
            IHttpClientFactory httpClient, ILogger<SiteflowService> logger, IMemoryCache cache)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _siteflowToken = _configuration["Siteflow:Token"];
            _baseURL = _configuration["Siteflow:BaseURL"];
            _siteflowSecret = _configuration["Siteflow:secret"];
            _cache = cache;
            _logger = logger;
        }

        // Generates the Authorization request header, given from https://developers.hp.com/hp-indigo-integration-hub/doc/hp-site-flow-api-authentication
        private string BuildHmacHeader(string method, string path, string timestamp, string token, string secret)
        {
            string stringToSign = $"{method} {Uri.UnescapeDataString(path)} {timestamp}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));

            string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();

            return $"{token}:{signature}";
        }
        
        /// <summary>
        /// Test to retrieve Siteflow products first
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<SiteflowApiResponse?> GetSiteflowStockProducts()
        {
            try
            {
                var client = _httpClient.CreateClient("siteflow");
                client.BaseAddress = new Uri(_baseURL!);
                
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var signature = BuildHmacHeader("GET", "/api/stock", timestamp, _siteflowToken!, _siteflowSecret!);

                client.DefaultRequestHeaders.Remove("x-oneflow-date");
                client.DefaultRequestHeaders.Remove("x-oneflow-authorization");
                client.DefaultRequestHeaders.Add("x-oneflow-date", timestamp);
                client.DefaultRequestHeaders.Add("x-oneflow-authorization", signature);

                // Send request
                var response = await client.GetFromJsonAsync<SiteflowApiResponse>("stock");

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed to Siteflow API");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing Siteflow API response");
                return null;
            }
        }

        /// <summary>
        /// Gets the data from the specific Siteflow product given sku/code
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<SiteflowDataRaw?> GetSiteflowStockProduct(CSVData data)
        {
            try
            {
                var client = _httpClient.CreateClient("siteflow");
                client.BaseAddress = new Uri(_baseURL!);

                // Get all items
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var signature = BuildHmacHeader("GET", "/api/stock", timestamp, _siteflowToken!, _siteflowSecret!);

                client.DefaultRequestHeaders.Remove("x-oneflow-date");
                client.DefaultRequestHeaders.Remove("x-oneflow-authorization");
                client.DefaultRequestHeaders.Add("x-oneflow-date", timestamp);
                client.DefaultRequestHeaders.Add("x-oneflow-authorization", signature);

                // Send request
                var response = await client.GetFromJsonAsync<SiteflowApiResponse>("stock");

                // Filter out the response to find the matching SKU/code
                var item = response?
                    .Data?
                    .FirstOrDefault(p => p.Code == data.Sku || p.Barcode == data.Sku);

                return item; 
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed to Siteflow API");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing Siteflow API response");
                return null;
            }
        }


        /// <summary>
        /// Data synchronization method for Siteflow. Updates stock quantity based on CSVData input.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SyncData(CSVData data)
        {
            _logger.LogInformation("Syncing data for SKU: {Sku}, Quantity: {Quantity}", data.Sku, data.Quantity);

            var stopwatch = Stopwatch.StartNew();

            var client = _httpClient.CreateClient("siteflow");
            client.BaseAddress = new Uri(_baseURL!);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<SiteflowDataRaw> products))
            {
                _logger.Log(LogLevel.Information, "Product found in cache");
            }
            else
            {
                _logger.Log(LogLevel.Information, "Product not found in cache, fetching from Siteflow API");

                // Get all items
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var signature = BuildHmacHeader("GET", "/api/stock", timestamp, _siteflowToken!, _siteflowSecret!);

                client.DefaultRequestHeaders.Remove("x-oneflow-date");
                client.DefaultRequestHeaders.Remove("x-oneflow-authorization");
                client.DefaultRequestHeaders.Add("x-oneflow-date", timestamp);
                client.DefaultRequestHeaders.Add("x-oneflow-authorization", signature);

                // Send request
                var response = await client.GetFromJsonAsync<SiteflowApiResponse>("stock");

                if (response?.Data == null)
                {
                    _logger.LogError("Failed to fetch products from Siteflow API");
                    return false;
                }

                products = response.Data;

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(4))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(10))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(cacheKey, products, cacheEntryOptions);
                _logger.LogInformation("Products cached successfully");
            }

            // Find specific item
            var item = products?.FirstOrDefault(p => p.Code == data.Sku || p.Barcode == data.Sku);

            if (item == null)
            {
                _logger.LogError("Item not found for SKU: {Sku}", data.Sku);
                return false;
            }

            // Call the API to update the stock quantity
            var updateTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var updateSignature = BuildHmacHeader("PUT", $"/api/stock/{item.Id}", updateTimestamp, _siteflowToken!, _siteflowSecret!);

            client.DefaultRequestHeaders.Remove("x-oneflow-date");
            client.DefaultRequestHeaders.Remove("x-oneflow-authorization");
            client.DefaultRequestHeaders.Add("x-oneflow-date", updateTimestamp);
            client.DefaultRequestHeaders.Add("x-oneflow-authorization", updateSignature);

            var payload = new { stockLevel = data.Quantity };

            using var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var updateResponse = await client.PutAsync($"stock/{item.Id}", jsonContent);

            var content = await updateResponse.Content.ReadAsStringAsync();
            if (updateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Stock updated successfully for {Sku}", data.Sku);
            }
            else
            {
                _logger.LogError("Failed to update stock for {Sku}. Status: {StatusCode}. Response: {Response}",
                    data.Sku, updateResponse.StatusCode, content);
            }

            _logger.LogInformation("Found item {Sku} after {Elapsed} ms", data.Sku, stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
            return updateResponse.IsSuccessStatusCode;

            
        }
    }
}
