using InventorySync.Models;
using InventorySync.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
            _siteflowToken = _configuration["Siteflow:Admin:Token"];
            _siteflowSecret = _configuration["Siteflow:Admin:secret"];
            _cache = cache;
            _logger = logger;
        }

        // Generates the Authorization request header, given from https://developers.hp.com/hp-indigo-integration-hub/doc/hp-site-flow-api-authentication
        private string BuildHmacHeader(string method, string path, string timestamp, string token, string secret)
        {
            string stringToSign = $"{method.ToUpper()} {Uri.UnescapeDataString(path)} {timestamp}";
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();
            return $"{token}:{signature}";
        }

        /// <summary>
        /// Test to retrieve Siteflow products first
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<List<SiteflowDataRaw>> GetAllSiteflowProducts(string targetSite)
        {
            var allProducts = new List<SiteflowDataRaw>();
            try
            {
                var client = _httpClient.CreateClient("siteflow");

                int page = 1;
                int pageSize = 1000; // max per request
                bool hasMore = true;

                while (page < 3)
                {
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    var path = "/api/stock"; // only path, no query parameters
                    var signature = BuildHmacHeader("GET", path, timestamp, _siteflowToken!, _siteflowSecret!);

                    client.DefaultRequestHeaders.Remove("x-oneflow-date");
                    client.DefaultRequestHeaders.Remove("x-oneflow-authorization");

                    client.DefaultRequestHeaders.Add("x-oneflow-date", timestamp);
                    client.DefaultRequestHeaders.Add("x-oneflow-authorization", signature);

                    var response = await client.GetFromJsonAsync<SiteflowApiResponse>(
                        $"stock?active=true&page={page}&pagesize=1000" // query parameters here
                    );

                    if (response?.Data != null && response.Data.Any())
                    {
                        allProducts.AddRange(response.Data);

                        var testItem = "US-COV-2500305";

                        var item = allProducts.FirstOrDefault(p => p.Code == testItem);

                        Console.WriteLine(item.Code, item.Id);

                        page++; // next page
                    }
                }

                Console.WriteLine($"Total products fetched: {allProducts.Count}");
                return allProducts;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed to Siteflow API");
                return allProducts;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing Siteflow API response");
                return allProducts;
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
                client.DefaultRequestHeaders.Remove("x-oneflow-algorithm");
                client.DefaultRequestHeaders.Add("x-oneflow-algorithm", "SHA256");

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
        public async Task<bool> SyncData(CSVData data, string targetSite)
        {
            _logger.LogInformation("Syncing data for SKU: {Sku}, Quantity: {Quantity}", data.Sku, data.Quantity);
            var stopwatch = Stopwatch.StartNew();

            var client = _httpClient.CreateClient("siteflow");

            List<SiteflowDataRaw> allProducts;
            if (_cache.TryGetValue(cacheKey, out IEnumerable<SiteflowDataRaw> cachedProducts))
            {
                _logger.LogInformation("Products found in cache");
                allProducts = cachedProducts.ToList(); // make sure we have a List<T>
            }
            else
            {
                _logger.Log(LogLevel.Information, "Products not found in cache, fetching from Siteflow API");
                allProducts = new List<SiteflowDataRaw>();
                int page = 1;
                int pageSize = 1000; // max per request

                while (page < 3)
                {
                    // Get all items
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    var path = "/api/stock"; // only path, no query parameters
                    var signature = BuildHmacHeader("GET", path, timestamp, _siteflowToken!, _siteflowSecret!);

                    client.DefaultRequestHeaders.Remove("x-oneflow-date");
                    client.DefaultRequestHeaders.Remove("x-oneflow-authorization");

                    client.DefaultRequestHeaders.Add("x-oneflow-date", timestamp);
                    client.DefaultRequestHeaders.Add("x-oneflow-authorization", signature);

                    var response = await client.GetFromJsonAsync<SiteflowApiResponse>(
                        $"stock?active=true&page={page}&pagesize=1000" // query parameters here
                    );

                    if (response?.Data != null && response.Data.Any())
                    {
                        allProducts.AddRange(response.Data);
                        _logger.LogInformation("Fetched page {Page} with {Count} items", page, response.Data.Count);
                        page++;
                    }
                    else
                    {
                        break;
                    }

                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(4))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(10))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(cacheKey, allProducts, cacheEntryOptions);
                _logger.LogInformation("Products cached successfully");
            }

            // Find specific item
            var item = allProducts.FirstOrDefault(p => p.Code == data.Sku || p.Barcode == data.Sku);
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

            stopwatch.Stop();
            return updateResponse.IsSuccessStatusCode;
        }
    }
}
