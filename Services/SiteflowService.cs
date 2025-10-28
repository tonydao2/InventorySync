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
        private readonly IConfiguration _configuration;
        private readonly string? _baseURL;
        private readonly ILogger _logger;

        public SiteflowService(IConfiguration configuration, IHttpClientFactory httpClient, ILogger<SiteflowService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _siteflowhmacHeader = _configuration["Siteflow:HmacKey"];
            _baseURL = _configuration["Siteflow:BaseURL"];
            _siteflowSecret = _configuration["Siteflow:secret"];
            _logger = logger;
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

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"); // seconds only
                var method = "GET";
                var path = "/api/stock"; // exact path for signature

                var token = _siteflowhmacHeader ?? throw new InvalidOperationException("Token not configured");
                var secret = _siteflowSecret ?? throw new InvalidOperationException("Secret not configured");

                // String to sign
                var stringToSign = $"{method} {path} {timestamp}";

                // HMAC SHA256
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                var signature = Convert.ToHexString(signatureBytes).ToLower();

                var authHeader = $"{token}:{signature}";

                // Create request
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/stock");
                request.Headers.Add("x-oneflow-authorization", authHeader);
                request.Headers.Add("x-oneflow-date", timestamp);
                request.Headers.Add("x-oneflow-algorithm", "SHA256");
                request.Headers.Add("Accept", "application/json");

                // Send request
                var httpResponse = await client.SendAsync(request);

                _logger.LogInformation("Siteflow API responded with status code: {StatusCode}", httpResponse.StatusCode);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Siteflow API returned error: {Content}", errorContent);
                    return null;
                }

                // 6️ Deserialize response
                var response = await httpResponse.Content.ReadFromJsonAsync<SiteflowApiResponse>();
                if (response == null)
                    _logger.LogWarning("Siteflow API returned empty response");

                return response;


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
