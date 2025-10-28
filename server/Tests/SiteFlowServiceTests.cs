using System;
using InventorySync.Services;
using InventorySync.Models;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventorySync.Tests
{
    public class SiteflowServiceTests
    {
        [Fact]
        public void TestSecret_LogsCorrectValues()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SiteflowService>>();


            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("secrets.json", optional: false, reloadOnChange: true)
                .Build();

            //var service = new SiteflowService(configuration, mockHttpClient, mockLogger.Object);

            //var result = service.TestSecret();
            //Console.WriteLine($"BaseURL: {result._baseUrl}, HmacKey: {result.siteflowhmacHeader}");


        }
    }
}
