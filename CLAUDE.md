# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the full solution (preferred)
dotnet build Plugin.Maui.Microchart.slnx --configuration Release

# Pack all NuGet packages (macOS/Linux)
./buildpackages-maui.sh

# Clean all bin/obj directories
./clean.sh

# Pack individual packages
dotnet pack src/Plugin.Maui.Microchart.Core/Plugin.Maui.Microchart.Core.csproj --configuration Release
dotnet pack src/Plugin.Maui.Microchart/Plugin.Maui.Microchart.csproj --configuration Release
dotnet pack src/Plugin.Maui.Microchart.Droid/Plugin.Maui.Microchart.Droid.csproj --configuration Release
dotnet pack src/Plugin.Maui.Microchart.iOS/Plugin.Maui.Microchart.iOS.csproj --configuration Release
dotnet pack src/Plugin.Maui.Microchart.Metapackage/Plugin.Maui.Microchart.Metapackage.csproj --configuration Release
```

Required .NET workloads: `android`, `ios`, `maccatalyst`, `maui`.

There are no test projects in this repository.

## Architecture

Plugin.Maui.Microchart is a cross-platform charting library built on **Microsoft.Maui.Graphics** targeting **.NET 10** — no SkiaSharp anywhere in the dependency graph. All chart rendering is platform-agnostic `ICanvas`/`IDrawable` drawing; platform projects provide thin `ChartView` wrappers.

### Project Dependency Graph

```
Plugin.Maui.Microchart.Core (net10.0, net10.0-ios, net10.0-android, net10.0-maccatalyst, net10.0-windows)
  ^           ^            ^
  |           |            |
  Maui      iOS         Droid
  (ChartView wrappers: Maui's is a GraphicsView/IDrawable; iOS/Droid are
   native UIView/View subclasses that draw through Maui.Graphics.Platform.PlatformCanvas)
```

`Plugin.Maui.Microchart.All` is a convenience umbrella package (Core + MAUI) built from a `.nuspec` file with target-framework-specific dependency groups; most MAUI apps should install `Plugin.Maui.Microchart` directly instead.

### Chart Class Hierarchy

All charts inherit from `Chart` (abstract base in `src/Plugin.Maui.Microchart.Core/Charts/Chart.cs`):

- **Chart** - Provides `Draw(ICanvas, RectF)`, animation, property change notification, and the `Invalidated` event that platform views subscribe to for re-rendering.
  - **SimpleChart** - Single-series charts: `PieChart`, `DonutChart`, `RadialGaugeChart`, `HalfRadialGaugeChart`, `RadarChart`
  - **SeriesChart** - Multi-series with `ChartSerie` collections
    - **PointChart** - `LineChart`
    - **AxisBasedChart** - `BarChart` (handles axis drawing, labels, grid)

### Rendering Pipeline

1. Platform `ChartView` subscribes to `Chart.Invalidated` via weak event handler (prevents memory leaks)
2. On invalidation, view calls platform-specific redraw (`Invalidate()` on MAUI's `GraphicsView`, `SetNeedsDisplayInRect()` on iOS, `PostInvalidate()` on Android)
3. `Chart.Draw()` fills background, then delegates to `DrawContent()` (abstract, implemented by each chart type)
4. Charts animate by interpolating `AnimationProgress` from 0 to 1 over `AnimationDuration`

### Data Model

- `ChartEntry` - A single data point (Value, Label, ValueLabel, Color)
- `ChartSerie` - Named collection of entries with optional color override

### Platform Views

MAUI's `ChartView` is a `GraphicsView` implementing `IDrawable`, with `BindableProperty` for XAML binding. The iOS/Droid `ChartView`s are native `UIView`/`View` subclasses that draw through `Microsoft.Maui.Graphics.Platform.PlatformCanvas` directly against the platform's own canvas. MAUI apps must call `UseMicrocharts()` on `MauiAppBuilder` (currently a no-op passthrough, kept for a consistent startup call site).

## Versioning and Packaging

Version is managed centrally in `src/Directory.Build.props` (`VersionMain` property). All package output goes to the `/artifacts` directory. The publish.yml workflow appends the GitHub run number as a prerelease suffix.

## CI/CD

- **pull-request.yml** - Runs on pull requests (and manual dispatch); builds on `windows-latest` and `macos-26` (with Xcode `latest-stable` via `maxim-lobanov/setup-xcode@v1`)
- **publish.yml** - Manual trigger (`workflow_dispatch`); always publishes packages to GitHub Packages (this repo's owner), and additionally to nuget.org when the `publish_to_nuget` input is set (OIDC trusted publishing)
