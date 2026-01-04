var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add CORS for browser-based speed testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpeedTest", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable CORS for speed test endpoints
app.UseCors("SpeedTest");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Speed test endpoints - NO throttling for real speed measurement
app.MapGet("/speedtest/download/{sizeInBytes:long}", (long sizeInBytes) =>
{
    // Limit chunk size to prevent abuse (max 100MB)
    var actualSize = Math.Min(sizeInBytes, 100_000_000);
    var bytes = new byte[actualSize];
    Random.Shared.NextBytes(bytes);
    
    return Results.Bytes(bytes, "application/octet-stream");
})
.WithName("DownloadTest")
.RequireCors("SpeedTest");

app.MapPost("/speedtest/upload", async (HttpRequest request) =>
{
    // Read and discard the uploaded data as fast as possible
    var buffer = new byte[1_048_576]; // 1MB buffer for fast reading
    long totalBytes = 0;
    
    while (true)
    {
        var bytesRead = await request.Body.ReadAsync(buffer);
        if (bytesRead == 0) break;
        totalBytes += bytesRead;
    }
    
    return Results.Ok(new { BytesReceived = totalBytes });
})
.WithName("UploadTest")
.RequireCors("SpeedTest");

app.MapGet("/speedtest/ping", () =>
{
    return Results.Ok(new { Timestamp = DateTime.UtcNow.Ticks });
})
.WithName("PingTest")
.RequireCors("SpeedTest");

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
