using InventorySync.Services;
using InventorySync.Services.Interfaces;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); 
builder.Logging.AddDebug();

// Add services to the container.
// Configure HttpClient for Siteflow API
builder.Services.AddHttpClient();
//builder.Services.AddHttpClient("siteflow", (serviceProvider, httpClient) =>
//{
//    var config = serviceProvider.GetRequiredService<IConfiguration>();
//    var baseUrl = config["Siteflow:BaseURL"];
//    var hmacKey = config["Siteflow:HmacKey"];
//    var requestDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

//    httpClient.BaseAddress = new Uri(baseUrl!);
    
//    //httpClient.DefaultRequestHeaders.Add("Accept", "application/json"); // Ensure we accept JSON responses
//    httpClient.DefaultRequestHeaders.Add("x-oneflow-authorization", hmacKey);
//    httpClient.DefaultRequestHeaders.Add("x-oneflow-date", requestDate);
//});

builder.Services.AddSingleton<ISiteflowSerivce, SiteflowService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Configuration.AddEnvironmentVariables(); // Load configuration from environment variables later for production

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("InventorySync")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
