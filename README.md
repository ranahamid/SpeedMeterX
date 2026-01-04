# SpeedMeterX

A modern, real-time internet speed test application built with **Blazor** and **.NET MAUI**. Test your download speed, upload speed, and ping latency with a beautiful, responsive UI. Available for Web, Android, iOS, macOS, and Windows.

![SpeedMeterX Screenshot](docs/screenshot.png)

## Features

- **Download Speed Test** - Measures download speed using Cloudflare's global CDN
- **Upload Speed Test** - Tests upload bandwidth (when CORS permits)
- **Ping/Latency Test** - Measures round-trip time to the server
- **Pause/Resume** - Pause the test and resume where you left off
- **Beautiful UI** - Animated speed gauges with real-time updates
- **Responsive Design** - Works on desktop and mobile devices
- **Auto-start** - Test begins automatically when you open the app
- **Cross-Platform** - Web, Android, iOS, macOS, and Windows

## Platforms

| Platform | Project | Status |
|----------|---------|--------|
| Web (Blazor Server) | SpeedMeterX.Web | Ready |
| Android | SpeedMeterX.Mobile | Ready |
| iOS | SpeedMeterX.Mobile | Ready |
| macOS (Catalyst) | SpeedMeterX.Mobile | Ready |
| Windows | SpeedMeterX.Mobile | Ready |

## Tech Stack

- **Web Frontend**: Blazor Server (.NET 10)
- **Mobile/Desktop**: .NET MAUI Blazor Hybrid
- **Backend**: ASP.NET Core Minimal APIs
- **Infrastructure**: .NET Aspire for orchestration
- **Speed Test Server**: Cloudflare CDN (external)
- **Styling**: CSS with animations

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.12+) with:
  - ASP.NET and web development workload
  - .NET MAUI workload (for mobile builds)
- For iOS builds: macOS with Xcode
- For Android builds: Android SDK (installed via Visual Studio)

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/ranahamid/SpeedMeterX.git
cd SpeedMeterX
```

### Run Web App with .NET Aspire

```bash
cd SpeedMeterX.AppHost
dotnet run
```

This will start the Aspire dashboard and all services. Open the URL shown in the console (typically `https://localhost:7200`).

### Run Mobile App

#### Android

```bash
cd SpeedMeterX.Mobile
dotnet build -t:Run -f net10.0-android
```

Or in Visual Studio:
1. Set `SpeedMeterX.Mobile` as the startup project
2. Select an Android emulator or device
3. Press F5 to run

#### iOS (requires macOS)

```bash
cd SpeedMeterX.Mobile
dotnet build -t:Run -f net10.0-ios
```

Or in Visual Studio for Mac / VS with Mac build host:
1. Set `SpeedMeterX.Mobile` as the startup project
2. Select an iOS simulator or device
3. Press F5 to run

#### Windows

```bash
cd SpeedMeterX.Mobile
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

#### macOS (Catalyst)

```bash
cd SpeedMeterX.Mobile
dotnet build -t:Run -f net10.0-maccatalyst
```

## Project Structure

```
SpeedMeterX/
|-- SpeedMeterX.AppHost/         # .NET Aspire orchestration
|-- SpeedMeterX.Web/             # Blazor Server frontend (Web)
|   |-- Components/
|   |   |-- Pages/
|   |   |   |-- Home.razor       # Main speed test page
|   |   |-- SpeedGauge.razor     # Speed gauge component
|   |-- wwwroot/
|       |-- js/
|           |-- speedtest.js     # Client-side speed test logic
|-- SpeedMeterX.Mobile/          # .NET MAUI Blazor Hybrid (iOS/Android/Windows/macOS)
|   |-- Components/
|   |   |-- Pages/
|   |   |   |-- Home.razor       # Mobile speed test page
|   |   |-- SpeedGauge.razor     # Speed gauge component
|   |-- wwwroot/
|   |-- Resources/               # App icons, splash screens
|-- SpeedMeterX.ApiService/      # Backend API service
|-- SpeedMeterX.ServiceDefaults/ # Shared service configuration
|-- SpeedMeterX.Tests/           # Unit tests
```

## Publishing

### Publish for Android (APK/AAB)

```bash
cd SpeedMeterX.Mobile
dotnet publish -f net10.0-android -c Release
```

The APK will be in `bin/Release/net10.0-android/publish/`

### Publish for iOS (IPA)

```bash
cd SpeedMeterX.Mobile
dotnet publish -f net10.0-ios -c Release
```

**Note**: iOS publishing requires a valid Apple Developer account and provisioning profile.

### Publish for Windows (MSIX)

```bash
cd SpeedMeterX.Mobile
dotnet publish -f net10.0-windows10.0.19041.0 -c Release
```

## How It Works

### Speed Test Flow

1. **Ping Test**: Sends 5 requests to Cloudflare and calculates average latency
2. **Download Test**: Downloads chunks of data for 10 seconds, adapting chunk size based on speed
3. **Upload Test**: Uploads random data to Cloudflare (may be blocked by CORS in some browsers)

### Technical Details

- **Client-side JavaScript** runs the actual speed tests directly from the WebView
- **Adaptive chunk sizing** increases download/upload chunk sizes based on measured speed
- **Warmup phase** excludes initial chunks from calculations for accuracy
- **Outlier removal** trims top/bottom 10% of samples for stable results
- **Real-time progress** updates via Blazor interop

## Configuration

### Test Duration

Edit `Home.razor` to change the test duration (default: 10 seconds per test):

```csharp
_downloadResult = await JSRuntime.InvokeAsync<SpeedResultJs>(
    "SpeedTest.measureDownload", 
    _dotNetRef, 
    10000);  // Duration in milliseconds
```

### App Icons

Replace the SVG files in `SpeedMeterX.Mobile/Resources/AppIcon/`:
- `appicon.svg` - App icon background
- `appiconfg.svg` - App icon foreground

### Splash Screen

Replace `SpeedMeterX.Mobile/Resources/Splash/splash.svg` with your custom splash screen.

## Screenshots

### Speed Test Running
![Running](docs/running.png)

### Test Complete
![Complete](docs/complete.png)

### Mobile App
![Mobile](docs/mobile.png)

## Known Limitations

- **Upload Test CORS**: Cloudflare's upload endpoint may block requests from some origins due to CORS policy. When this happens, upload speed shows as "N/A".
- **iOS Background**: Speed tests may be interrupted when the app goes to background on iOS.

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
- [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) for cross-platform development
- [Blazor](https://blazor.net/) for the amazing web framework

## Author

Hamid - [@ranahamid](https://github.com/ranahamid)

Project Link: [https://github.com/ranahamid/SpeedMeterX](https://github.com/ranahamid/SpeedMeterX)
