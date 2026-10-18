# PulseSlide

PulseSlide is a Windows desktop audio visualizer that slides in from a screen edge when Windows output audio is detected.

## Features
- Detects Windows output audio through WASAPI loopback (music, games, notification sounds).
- Four display edges: top, bottom, left, right.
- Three visual styles: Neon Bars, Wave, Spectrum.
- Adjustable sensitivity, glow color, and hide delay.
- Test button for preview.
- GitHub Actions workflow publishes a self-contained Windows x64 `.exe`.

## Build locally
Install the .NET 8 SDK, then run in this directory:

```powershell
dotnet publish PulseSlide.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Run `publish\PulseSlide.exe`.

## Download the EXE
After GitHub Actions completes, open the repository's **Actions** tab, select the successful build, and download the `PulseSlide-Windows-x64` artifact. Extract the ZIP and run `PulseSlide.exe`.

## Notes
- The app listens to the default Windows playback device.
- This initial version targets the primary display.
- Windows may show a SmartScreen warning for unsigned applications.
