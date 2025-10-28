using InventorySync.Models;
using InventorySync.Services.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace InventorySync.Services
{
    public class SiteflowService : ISiteflowSerivce
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly string? _siteflowhmacHeader;
        private readonly string? _siteflowSecret;
        private readonly string? _siteflowToken;
        private readonly IConfiguration _configuration;
        private readonly string? _baseURL;
        private readonly ILogger _logger;

        public SiteflowService(IConfiguration configuration, IHttpClientFactory httpClient, ILogger<SiteflowService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _siteflowhmacHeader = _configuration["Siteflow:HmacKey"];
            _siteflowToken = _configuration["Siteflow:Token"];
            _baseURL = _configuration["Siteflow:BaseURL"];
            _siteflowSecret = _configuration["Siteflow:secret"];
            _logger = logger;
        }

        
        private string BuildHmacHeader(string method, string path, string timestamp, string token, string secret)
        {
            // Note: must use "METHOD PATH TIMESTAMP" — spaces, not newlines
            string stringToSign = $"{method} {Uri.UnescapeDataString(path)} {timestamp}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));

            // Convert to lowercase hex string (NOT Base64)
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
                client.BaseAddress = new Uri(_baseURL!); // https://pro-api.oneflowcloud.com/api/
                var requestDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var method = "GET";
                var path = "/api/stock"; // EXACT PATH (no domain, no encoding)

                var signature = BuildHmacHeader(method, path, timestamp, _siteflowToken!, _siteflowSecret!);


                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("x-oneflow-date", timestamp);
                client.DefaultRequestHeaders.Add("x-oneflow-algorithm", "SHA256"); // ✅ THIS WAS MISSING
                client.DefaultRequestHeaders.Add("x-oneflow-authorization", signature);

                // Send request
                var httpResponse = await client.GetFromJsonAsync<SiteflowApiResponse>("stock");

                return httpResponse;
                
                //var response = await client.GetFromJsonAsync<SiteflowApiResponse>("stock");

                //return response;
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

        public Task<bool> SyncData(CSVData item)
        {
            throw new NotImplementedException();
        }
    }
}
