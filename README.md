# SpeedMeterX ??

A modern, real-time internet speed test application built with **Blazor** and **.NET Aspire**. Test your download speed, upload speed, and ping latency with a beautiful, responsive UI.

![SpeedMeterX Screenshot](docs/screenshot.png)

## Features

- ?? **Download Speed Test** - Measures download speed using Cloudflare's global CDN
- ?? **Upload Speed Test** - Tests upload bandwidth (when CORS permits)
- ?? **Ping/Latency Test** - Measures round-trip time to the server
- ?? **Pause/Resume** - Pause the test and resume where you left off
- ?? **Beautiful UI** - Animated speed gauges with real-time updates
- ?? **Responsive Design** - Works on desktop and mobile devices
- ?? **Auto-start** - Test begins automatically when you open the page

## Tech Stack

- **Frontend**: Blazor Server (.NET 10)
- **Backend**: ASP.NET Core Minimal APIs
- **Infrastructure**: .NET Aspire for orchestration
- **Speed Test Server**: Cloudflare CDN (external)
- **Styling**: CSS with animations

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.12+) or [VS Code](https://code.visualstudio.com/) with C# extension
- Docker Desktop (for .NET Aspire)

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/yourusername/SpeedMeterX.git
cd SpeedMeterX
```

### Run with .NET Aspire

```bash
cd SpeedMeterX.AppHost
dotnet run
```

This will start the Aspire dashboard and all services. Open the URL shown in the console (typically `https://localhost:7200`).

### Run Individually

To run just the web application:

```bash
cd SpeedMeterX.Web
dotnet run
```

## Project Structure

```
SpeedMeterX/
??? SpeedMeterX.AppHost/        # .NET Aspire orchestration
??? SpeedMeterX.Web/            # Blazor Server frontend
?   ??? Components/
?   ?   ??? Pages/
?   ?   ?   ??? Home.razor      # Main speed test page
?   ?   ??? SpeedGauge.razor    # Speed gauge component
?   ?   ??? Layout/
?   ??? wwwroot/
?       ??? js/
?           ??? speedtest.js    # Client-side speed test logic
??? SpeedMeterX.ApiService/     # Backend API service
??? SpeedMeterX.ServiceDefaults/# Shared service configuration
??? SpeedMeterX.Tests/          # Unit tests
```

## How It Works

### Speed Test Flow

1. **Ping Test**: Sends 5 requests to Cloudflare and calculates average latency
2. **Download Test**: Downloads chunks of data for 10 seconds, adapting chunk size based on speed
3. **Upload Test**: Uploads random data to Cloudflare (may be blocked by CORS in some browsers)

### Technical Details

- **Client-side JavaScript** runs the actual speed tests directly from the browser
- **Adaptive chunk sizing** increases download/upload chunk sizes based on measured speed
- **Warmup phase** excludes initial chunks from calculations for accuracy
- **Outlier removal** trims top/bottom 10% of samples for stable results
- **Real-time progress** updates via Blazor SignalR connection

## Configuration

### Test Duration

Edit `Home.razor` to change the test duration (default: 10 seconds per test):

```csharp
_downloadResult = await JSRuntime.InvokeAsync<SpeedResultJs>(
    "SpeedTest.measureDownload", 
    _dotNetRef, 
    10000);  // Duration in milliseconds
```

### Speed Test Endpoints

The app uses Cloudflare's public speed test endpoints. To use your own server, modify `speedtest.js`:

```javascript
cloudflareDownload: 'https://your-server.com/download?bytes=',
cloudflareUpload: 'https://your-server.com/upload',
```

## Screenshots

### Speed Test Running
![Running](docs/running.png)

### Test Complete
![Complete](docs/complete.png)

### Paused State
![Paused](docs/paused.png)

## Known Limitations

- **Upload Test CORS**: Cloudflare's upload endpoint may block requests from some origins due to CORS policy. When this happens, upload speed shows as "N/A".
- **Browser Throttling**: Some browsers may throttle background tabs, affecting test accuracy if the tab is not focused.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Cloudflare](https://speed.cloudflare.com/) for providing public speed test endpoints
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) for cloud-native orchestration
- [Blazor](https://blazor.net/) for the amazing web framework

## Author

Your Name - [@yourusername](https://twitter.com/yourusername)

Project Link: [https://github.com/yourusername/SpeedMeterX](https://github.com/yourusername/SpeedMeterX)
