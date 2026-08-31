# Plugin.Maui.Microchart

[![PR Build](https://github.com/aminparsa18/Plugin.Maui.Microchart/actions/workflows/pull-request.yml/badge.svg)](https://github.com/aminparsa18/Plugin.Maui.Microchart/actions/workflows/pull-request.yml)
[![NuGet](https://img.shields.io/nuget/v/Plugin.Maui.Microchart.svg)](https://www.nuget.org/packages/Plugin.Maui.Microchart/)

**Plugin.Maui.Microchart** is a simple charting library for .NET MAUI, iOS, and Android, drawn entirely with **Microsoft.Maui.Graphics** — no SkiaSharp dependency.

## About

This is a fork of [Microcharts](https://github.com/microcharts-dotnet/Microcharts) reworked to render through `Microsoft.Maui.Graphics` (`GraphicsView`/`IDrawable` on MAUI, `PlatformCanvas` natively on iOS/Android) instead of SkiaSharp. It started as [a proposal to land the rewrite upstream](https://github.com/microcharts-dotnet/Microcharts/issues/353); with no response from the Microcharts maintainers, it's published here as its own package instead.

This is a breaking change from Microcharts, not a drop-in upgrade: the `Legacy*` chart classes are gone, SkiaSharp is gone from the public surface, and the namespace is now `Plugin.Maui.Microchart` rather than `Microcharts`. All chart types are ported — pie, donut, radial gauge, radar, bar, and line — including the arc, gradient-stroke, and dashed-ring drawing that don't have a direct Maui.Graphics equivalent.

The purpose is not to have a heavily customizable charting library. If you want that, simply fork the code — all of this is fairly simple.

Coming from Microcharts? See [Migrating from Microcharts](#migrating-from-microcharts) below.

## Gallery

![animation gallery](assets/animations.gif)

![gallery](assets/Gallery.png)

## Install

Available on NuGet:

* [Plugin.Maui.Microchart](https://www.nuget.org/packages/Plugin.Maui.Microchart/) — **start here for .NET MAUI apps.** MAUI `ChartView`, pulls in `.Core` automatically.
* [Plugin.Maui.Microchart.iOS](https://www.nuget.org/packages/Plugin.Maui.Microchart.iOS/) — native iOS `ChartView` (non-MAUI apps)
* [Plugin.Maui.Microchart.Droid](https://www.nuget.org/packages/Plugin.Maui.Microchart.Droid/) — native Android `ChartView` (non-MAUI apps)
* [Plugin.Maui.Microchart.Core](https://www.nuget.org/packages/Plugin.Maui.Microchart.Core/) — cross-platform rendering only, no platform view (pulled in automatically by the packages above)
* [Plugin.Maui.Microchart.All](https://www.nuget.org/packages/Plugin.Maui.Microchart.All/) — convenience umbrella package (Core + MAUI); most people don't need this, install `Plugin.Maui.Microchart` directly instead

Every build is also mirrored to the [GitHub Packages feed](https://github.com/aminparsa18?tab=packages) as a prerelease/CI channel.

**.NET MAUI**

`MauiProgram.cs`:

```csharp
using Plugin.Maui.Microchart;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMicrocharts();   // no-op today; call it anyway in case that changes

        return builder.Build();
    }
}
```

`MainPage.xaml`:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:charts="clr-namespace:Plugin.Maui.Microchart;assembly=Plugin.Maui.Microchart"
             x:Class="YourApp.MainPage">
    <charts:ChartView Chart="{Binding Chart}" HeightRequest="250" />
</ContentPage>
```

`MainPage.xaml.cs` (or your view model):

```csharp
using Plugin.Maui.Microchart;
using Microsoft.Maui.Graphics;

public Chart Chart { get; } = new PieChart
{
    Entries = new[]
    {
        new ChartEntry(30) { Label = "Coffee", Color = Colors.SaddleBrown },
        new ChartEntry(45) { Label = "Tea",    Color = Colors.ForestGreen },
        new ChartEntry(25) { Label = "Water",  Color = Colors.CornflowerBlue },
    },
};
```

## Migrating from Microcharts

1. **Swap the packages.** Uninstall `Microcharts` / `Microcharts.Core` / `Microcharts.Maui` (and `SkiaSharp` / `SkiaSharp.Views.Maui.Controls` if you referenced them directly), install `Plugin.Maui.Microchart` / `.Core` / `.iOS` / `.Droid` instead.

2. **Update the namespace.**

   ```diff
   -using Microcharts;
   +using Plugin.Maui.Microchart;
   ```

3. **Update XAML `xmlns` declarations.**

   ```diff
   -xmlns:charts="clr-namespace:Microcharts.Maui;assembly=Microcharts.Maui"
   +xmlns:charts="clr-namespace:Plugin.Maui.Microchart;assembly=Plugin.Maui.Microchart"
   ```

4. **Switch colors from `SKColor` to `Color`.** `ChartEntry.Color` (and `TextColor`, `ValueLabelColor`, `OtherColor`) are now `Microsoft.Maui.Graphics.Color`, not `SkiaSharp.SKColor`:

   ```diff
   -using SkiaSharp;
   +using Microsoft.Maui.Graphics;

    new ChartEntry(30)
    {
   -    Color = SKColor.Parse("#266489"),
   +    Color = Color.Parse("#266489"),
    };
   ```

5. **Drop any `Legacy*` charts.** `LegacyBarChart`, `LegacyLineChart`, and `LegacyPointChart` were removed rather than ported — move to their modern equivalents (`BarChart`, `LineChart`, `PointChart`) if you were still on those.

6. **Leave `UseMicrocharts()` in `MauiProgram.cs` as-is** — the method name didn't change, it's just a no-op now that `GraphicsView`'s handler is registered by MAUI automatically.

Everything else — chart types, property names (`Entries`, `Label`, `ValueLabel`, animation properties, etc.) — is unchanged.

## Compatibility

Built-in views are provided for the following, all targeting .NET 10 and rendered with Microsoft.Maui.Graphics:

* .NET MAUI (Windows, Android, iOS, and Mac Catalyst)
* .NET for iOS — native `Plugin.Maui.Microchart.iOS` view
* .NET for Android — native `Plugin.Maui.Microchart.Droid` view

## Contributions

Contributions are welcome! If you find a bug please report it, and if you want a feature please open an issue for it first.

If you want to contribute code, please branch off of `main` and file a pull request.

## License

MIT © [Amin Parsa](https://github.com/aminparsa18). Based on [Microcharts](https://github.com/microcharts-dotnet/Microcharts) by [Aloïs Deniel](https://aloisdeniel.com), [Ed Lomonaco](https://edlomonaco.dev) & [Jonas Follesø](https://github.com/follesoe).
