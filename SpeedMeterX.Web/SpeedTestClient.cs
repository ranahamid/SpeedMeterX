using System.Diagnostics;
using System.Net.Http.Headers;

namespace SpeedMeterX.Web;

public class SpeedTestClient(HttpClient httpClient)
{
    // Cloudflare speed test endpoints
    private const string CloudflareDownloadUrl = "https://speed.cloudflare.com/__down?bytes=";
    private const string CloudflareUploadUrl = "https://speed.cloudflare.com/__up";
    private const string CloudflarePingUrl = "https://speed.cloudflare.com/__down?bytes=0";

    private readonly HttpClient _externalClient = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    public async Task<PingResult> MeasurePingAsync(CancellationToken cancellationToken = default)
    {
        var samples = new List<long>();
        const int pingCount = 5;

        try
        {
            for (int i = 0; i < pingCount; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                using var request = new HttpRequestMessage(HttpMethod.Get, CloudflarePingUrl);
                using var response = await _externalClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    samples.Add(stopwatch.ElapsedMilliseconds);
                }

                if (i < pingCount - 1)
                {
                    await Task.Delay(200, cancellationToken);
                }
            }

            if (samples.Count == 0)
            {
                return new PingResult(0, false);
            }

            // Remove highest and lowest, then average
            if (samples.Count >= 3)
            {
                samples = samples.OrderBy(s => s).Skip(1).Take(samples.Count - 2).ToList();
            }

            var averageLatency = (long)samples.Average();
            return new PingResult(averageLatency, true);
        }
        catch
        {
            return new PingResult(0, false);
        }
    }

    public async Task<SpeedResult> MeasureDownloadSpeedAsync(
        IProgress<SpeedProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        const int testDurationMs = 10000;
        const int initialChunkSize = 100_000;      // 100 KB
        const int maxChunkSize = 25_000_000;       // 25 MB
        const int warmupChunks = 2;

        var stopwatch = Stopwatch.StartNew();
        long totalBytes = 0;
        var samples = new List<double>();
        int chunkCount = 0;
        int currentChunkSize = initialChunkSize;

        try
        {
            while (stopwatch.ElapsedMilliseconds < testDurationMs && !cancellationToken.IsCancellationRequested)
            {
                var chunkStart = Stopwatch.StartNew();

                using var request = new HttpRequestMessage(HttpMethod.Get, $"{CloudflareDownloadUrl}{currentChunkSize}");
                using var response = await _externalClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                // Read the content to ensure full download
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                chunkStart.Stop();
                chunkCount++;

                var bytesReceived = bytes.Length;

                // Skip warmup chunks from calculations
                if (chunkCount > warmupChunks)
                {
                    totalBytes += bytesReceived;

                    // Calculate instantaneous speed in Mbps
                    if (chunkStart.Elapsed.TotalSeconds > 0.01)
                    {
                        var instantSpeed = (bytesReceived * 8.0) / (chunkStart.Elapsed.TotalSeconds * 1_000_000);
                        samples.Add(instantSpeed);

                        // Adaptive chunk sizing - increase chunk size if speed is high
                        if (instantSpeed > 50 && currentChunkSize < maxChunkSize)
                        {
                            currentChunkSize = Math.Min(currentChunkSize * 2, maxChunkSize);
                        }
                    }
                }
                else
                {
                    // During warmup, still adapt chunk size
                    if (chunkStart.Elapsed.TotalSeconds > 0.01)
                    {
                        var warmupSpeed = (bytesReceived * 8.0) / (chunkStart.Elapsed.TotalSeconds * 1_000_000);
                        if (warmupSpeed > 20)
                        {
                            currentChunkSize = Math.Min(currentChunkSize * 4, maxChunkSize);
                        }
                    }
                }

                // Report progress
                var currentSpeed = samples.Count > 0 ? samples.TakeLast(5).Average() : 0;
                var progressPercent = Math.Min(100, (int)((stopwatch.ElapsedMilliseconds * 100) / testDurationMs));
                progress?.Report(new SpeedProgress(currentSpeed, progressPercent, "Downloading..."));
            }

            stopwatch.Stop();

            if (samples.Count == 0)
            {
                return new SpeedResult(0, 0, 0, stopwatch.Elapsed, false);
            }

            // Remove outliers (top and bottom 10%)
            var sortedSamples = samples.OrderBy(s => s).ToList();
            var trimCount = Math.Max(1, sortedSamples.Count / 10);
            var trimmedSamples = sortedSamples.Skip(trimCount).Take(Math.Max(1, sortedSamples.Count - (2 * trimCount))).ToList();

            var averageSpeed = trimmedSamples.Average();
            var maxSpeed = samples.Max();

            return new SpeedResult(averageSpeed, maxSpeed, totalBytes, stopwatch.Elapsed, true);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var averageSpeed = samples.Count > 0 ? samples.Average() : 0;
            return new SpeedResult(averageSpeed, 0, totalBytes, stopwatch.Elapsed, false);
        }
        catch (Exception)
        {
            return new SpeedResult(0, 0, 0, TimeSpan.Zero, false);
        }
    }

    public async Task<SpeedResult> MeasureUploadSpeedAsync(
        IProgress<SpeedProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        const int testDurationMs = 10000;
        const int initialChunkSize = 100_000;      // 100 KB
        const int maxChunkSize = 10_000_000;       // 10 MB
        const int warmupChunks = 2;

        var stopwatch = Stopwatch.StartNew();
        long totalBytes = 0;
        var samples = new List<double>();
        int chunkCount = 0;
        int currentChunkSize = initialChunkSize;

        // Pre-generate upload data
        var uploadBuffer = new byte[maxChunkSize];
        Random.Shared.NextBytes(uploadBuffer);

        try
        {
            while (stopwatch.ElapsedMilliseconds < testDurationMs && !cancellationToken.IsCancellationRequested)
            {
                var chunkStart = Stopwatch.StartNew();

                // Create content with current chunk size
                var content = new ByteArrayContent(uploadBuffer, 0, currentChunkSize);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using var request = new HttpRequestMessage(HttpMethod.Post, CloudflareUploadUrl)
                {
                    Content = content
                };

                using var response = await _externalClient.SendAsync(request, cancellationToken);

                chunkStart.Stop();
                chunkCount++;

                // Skip warmup chunks from calculations
                if (chunkCount > warmupChunks)
                {
                    totalBytes += currentChunkSize;

                    // Calculate instantaneous speed in Mbps
                    if (chunkStart.Elapsed.TotalSeconds > 0.01)
                    {
                        var instantSpeed = (currentChunkSize * 8.0) / (chunkStart.Elapsed.TotalSeconds * 1_000_000);
                        samples.Add(instantSpeed);

                        // Adaptive chunk sizing
                        if (instantSpeed > 20 && currentChunkSize < maxChunkSize)
                        {
                            currentChunkSize = Math.Min(currentChunkSize * 2, maxChunkSize);
                        }
                    }
                }
                else
                {
                    // During warmup, adapt chunk size
                    if (chunkStart.Elapsed.TotalSeconds > 0.01)
                    {
                        var warmupSpeed = (currentChunkSize * 8.0) / (chunkStart.Elapsed.TotalSeconds * 1_000_000);
                        if (warmupSpeed > 10)
                        {
                            currentChunkSize = Math.Min(currentChunkSize * 4, maxChunkSize);
                        }
                    }
                }

                // Report progress
                var currentSpeed = samples.Count > 0 ? samples.TakeLast(5).Average() : 0;
                var progressPercent = Math.Min(100, (int)((stopwatch.ElapsedMilliseconds * 100) / testDurationMs));
                progress?.Report(new SpeedProgress(currentSpeed, progressPercent, "Uploading..."));
            }

            stopwatch.Stop();

            if (samples.Count == 0)
            {
                return new SpeedResult(0, 0, 0, stopwatch.Elapsed, false);
            }

            // Remove outliers (top and bottom 10%)
            var sortedSamples = samples.OrderBy(s => s).ToList();
            var trimCount = Math.Max(1, sortedSamples.Count / 10);
            var trimmedSamples = sortedSamples.Skip(trimCount).Take(Math.Max(1, sortedSamples.Count - (2 * trimCount))).ToList();

            var averageSpeed = trimmedSamples.Average();
            var maxSpeed = samples.Max();

            return new SpeedResult(averageSpeed, maxSpeed, totalBytes, stopwatch.Elapsed, true);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var averageSpeed = samples.Count > 0 ? samples.Average() : 0;
            return new SpeedResult(averageSpeed, 0, totalBytes, stopwatch.Elapsed, false);
        }
        catch (Exception)
        {
            return new SpeedResult(0, 0, 0, TimeSpan.Zero, false);
        }
    }
}

public record PingResult(long LatencyMs, bool Success);

public record SpeedResult(double AverageSpeedMbps, double MaxSpeedMbps, long TotalBytes, TimeSpan Duration, bool Success);

public record SpeedProgress(double CurrentSpeedMbps, int ProgressPercent, string Status);

public record SpeedTestResults
{
    public PingResult? Ping { get; set; }
    public SpeedResult? Download { get; set; }
    public SpeedResult? Upload { get; set; }
    public DateTime TestDate { get; set; } = DateTime.Now;
}
