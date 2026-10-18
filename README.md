# PulseSlide

PulseSlide is a Windows desktop audio visualizer that slides in from a screen edge when Windows output audio is detected.

## Visualizer modes
- **Neon Bars** — glowing bars react to audio.
- **Waveform** — an oscilloscope-like waveform.
- **Spectrum** — frequency bands from low to high frequencies.
- **Circular / Radial** — frequency bars radiate around a central ring.
- **VU Meter** — a retro analog-style needle meter.
- **Beat Pulse** — bass-responsive pulsing rings and particle-like points.

## Features
- Captures default Windows playback audio using WASAPI loopback.
- Four display edges: top, bottom, left, right.
- Adjustable sensitivity, glow color, and hide delay.
- Test button previews every visualizer style.
- GitHub Actions publishes a self-contained Windows x64 `.exe`.

## Build locally
Install the .NET 8 SDK, then run:

```powershell
dotnet publish PulseSlide.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Run `publish\PulseSlide.exe`.

## Download
Open the repository's **Actions** tab, select the latest successful build, and download the `PulseSlide-Windows-x64` artifact. Extract the ZIP and run `PulseSlide.exe`.

## Notes
- The app listens to the default Windows playback device.
- This initial version targets the primary display.
- Windows may show a SmartScreen warning for unsigned applications.
