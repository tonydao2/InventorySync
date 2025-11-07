using InventorySync.Services;
using InventorySync.Services.Interfaces;
using Scalar.AspNetCore;
using System.Net.Http.Headers;
using System.Text;


var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
//builder.Logging.AddConsole(); 
//builder.Logging.AddDebug();

builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "[HH:mm:ss] ";
    //options.IncludeScopes = true;
    options.SingleLine = true;
});
builder.Logging.AddDebug();


// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});



// Add services to the container.
// Configure HttpClient for Siteflow API
builder.Services.AddHttpClient("siteflow", (serviceProvider, httpClient) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["Siteflow:BaseURL"];
    httpClient.BaseAddress = new Uri(baseUrl!);

    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("infigo", (serviceProvier, httpClient) =>
{
    var config = serviceProvier.GetRequiredService<IConfiguration>();
    var baseUrl = config["Infigo:BaseURL"];
    httpClient.BaseAddress = new Uri(baseUrl!);

    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<ISiteflowSerivce, SiteflowService>();
builder.Services.AddSingleton<IInfigoService, InfigoService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

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

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
