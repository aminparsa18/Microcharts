# Replace SkiaSharp with Microsoft.Maui.Graphics (Microcharts 3.0)

## Context

Microcharts currently renders every chart through SkiaSharp: `Chart.Draw(SKCanvas, int, int)` is the core drawing entry point, and `SKPaint`/`SKPath`/`SKColor`/`SKFont`/`SKShader` are used directly throughout `Sources/Microcharts/Charts/*.cs`, `Extensions/CanvasExtensions.cs`, and `Helpers/*.cs`. All three platform packages (`Microcharts.Maui`, `Microcharts.iOS`, `Microcharts.Droid`) wrap `SKCanvasView` and depend on `SkiaSharp.Views*`.

The user wants to fully replace this engine with `Microsoft.Maui.Graphics` (the `ICanvas`/`IDrawable`/`GraphicsView` abstraction MAUI ships) and remove the SkiaSharp dependency from the repo entirely — not add it as a second engine alongside Skia. This is an inherently breaking change to the core public API (`SKColor` → `Color`, `SKCanvas` → `ICanvas`, etc.), so it ships as **Microcharts 3.0.0**.

Decisions already made (see below) narrow the scope: the native `Microcharts.iOS`/`Microcharts.Droid` packages stay as pure-native (non-MAUI) packages rebuilt on Maui.Graphics' platform canvas types, rather than being dropped or folded into MAUI; the three obsolete `Legacy*` chart classes are deleted rather than ported; and the change lands as a major-version bump on the existing package IDs/namespaces on `main`, not a parallel package line.

## Locked-in decisions

1. **Microcharts.iOS / Microcharts.Droid**: rebuilt on Microsoft.Maui.Graphics' platform-native canvas types (not dropped, not left on Skia). All three platform packages (Maui/iOS/Droid) keep their current scope.
2. **Legacy charts** (`LegacyBarChart`, `LegacyLineChart`, `LegacyPointChart`): deleted, not ported.
3. **Versioning**: `VersionMain` in `Sources/Directory.Build.props` bumps `2.0.0` → `3.0.0`; same package IDs/namespaces, done on a feature branch merged to `main` when ready.

## Why this is more than a canvas swap

- SkiaSharp types are baked into the *public/internal API surface* of the core library, not hidden behind an internal abstraction — every chart class, `CanvasExtensions.cs`, and `Helpers/*.cs` talk `SKCanvas`/`SKPaint`/`SKPath`/`SKFont` directly.
- The single biggest risk: `SKFont.MeasureText` returns **ink-bounds** glyph rects + `SKFontMetrics.Ascent/Descent`, which drive nearly all label/legend/axis layout math (`Helpers/MeasureHelper.cs`, `Helpers/DrawHelper.cs`, `Extensions/CanvasExtensions.cs`). `Microsoft.Maui.Graphics.ICanvas.GetStringSize` only returns an advance-based `SizeF` — there is no portable ink-bounds/ascent-descent API.
- `LineChart`'s area-fill fade uses `SKShader.CreateCompose(shaderY, shaderX, SKBlendMode.SrcOut)` to combine two gradients via a blend mode — Maui.Graphics has no shader-composition primitive, only single-axis `LinearGradientPaint`/`RadialGradientPaint`.
- `Microcharts.iOS`/`Microcharts.Droid` are pure-native (no MAUI); `GraphicsView` is a MAUI-controls concept, so those two packages need to drive Maui.Graphics' platform canvas types directly rather than through `GraphicsView`.

## Architecture change

Change `Chart`'s abstract drawing surface from `Draw(SKCanvas, int width, int height)` / `DrawContent(SKCanvas, int, int)` to `Draw(ICanvas, RectF dirtyRect)` / `DrawContent(ICanvas, RectF)`, matching `IDrawable.Draw(ICanvas, RectF)` — this lets a `Chart` optionally be handed straight to a `GraphicsView.Drawable`. `DrawableChartArea` becomes `RectF`.

**Primary rewrite targets** (everything else funnels through these): `Sources/Microcharts/Extensions/CanvasExtensions.cs`, `Sources/Microcharts/Helpers/DrawHelper.cs`, `Sources/Microcharts/Helpers/MeasureHelper.cs`. Once these are ported, most concrete chart classes need only type-name swaps (`SKCanvas`→`ICanvas`, `SKColor`→`Color`, `SKPoint`→`PointF`, `SKRect`→`RectF`, `SKPath`→`PathF`, `SKPaint{Style=Fill,...}`→`canvas.FillColor=...; canvas.FillPath/FillRectangle(...)`, `SKPaint{Style=Stroke,...}`→`canvas.StrokeColor/StrokeSize; canvas.DrawPath/DrawLine(...)`), not logic rewrites.

**New abstraction — `ITextMetricsProvider`** (new file, e.g. `Sources/Microcharts/Abstracts/ITextMetricsProvider.cs`), exposing exactly what the codebase needs:
```csharp
RectF MeasureInkBounds(string text, IFont font, float fontSize);
(float Ascent, float Descent) GetFontMetrics(IFont font, float fontSize);
```
- Portable default implementation backed by `ICanvas.GetStringSize` + calibrated ratios (ascent ≈0.75×fontSize, descent ≈0.25×fontSize) — used by `Microcharts.Maui` unless wiring a precise one proves easy, and as the Windows fallback.
- Platform-accurate implementations for the two native packages: iOS via `UIFont.Ascender/.Descender` + `NSString.BoundingRect`; Android via `Paint.FontMetrics` + `Paint.GetTextBounds`. `Chart` gets a settable `ITextMetricsProvider` property (default = portable) that `Microcharts.iOS`/`Microcharts.Droid` inject with the precise variant.
- All internal call sites (`CanvasExtensions.DrawCaptionLabels`/`DrawTextCenteredVertically`, `DrawHelper.DrawLabel`, `MeasureHelper.MeasureTexts/CalculateYAxis`, `AxisBasedChart.GenerateSerieLegend`) get rewritten against this interface.

**Specific technique replacements:**
- `Chart.Draw`'s hard background clear (`SKPaint{BlendMode=Src}`, `Charts/Chart.cs:303-311`) → `canvas.FillColor = BackgroundColor; canvas.FillRectangle(dirtyRect);`. Verify no stale-content bleed-through with rapid opaque/transparent toggling (this exact bug was fixed recently for the Skia path — don't assume Maui.Graphics platform canvases are automatically safe).
- `LineChart`'s X-direction gradient (`EnableYFadeOutGradient=false`, the default) ports directly to a multi-stop `LinearGradientPaint`. The Y-fade path (`EnableYFadeOutGradient=true`) has no faithful equivalent — ship a documented visual simplification (single-axis alpha-varying vertical gradient, sacrificing the 2D X+Y combination) since it's an opt-in, default-off feature. Call this out explicitly as the one intentional behavior change in the 3.0 release notes.
- `RadarChart`'s dashed rings (`SKPathEffect.CreateDash`) → `canvas.StrokeDashPattern`/`StrokeDashOffset` (direct 1:1, same even/odd dash-gap semantics). Its `SKMatrix.CreateRotation(angle).MapPoint(point)` (2 call sites) → a small private static trig helper (`RotatePoint`) in `RadarChart.cs`, no general matrix type needed. Its circular clip (`SKPath.AddCircle` + `ClipPath`) → `PathF.AppendCircle(center, radius)` + `canvas.ClipPath(path)`.
- `DonutChart`/`RadialHelpers`' full-circle hole punch (`SKPathFillType.EvenOdd` on two overlapping circles) ports almost verbatim: Maui.Graphics' `ICanvas.FillPath(PathF, WindingMode)` accepts `WindingMode.EvenOdd` as a draw-time argument (not a path property) — same two-circle `PathF` construction, just pass the winding mode to `FillPath`. The partial-sector path (`ArcTo` SVG endpoint-style) gets re-derived against `PathF.AddArc`'s center/bounding-box+angle signature (`RadialHelpers` already computes from center+radius+angle, so this is a natural fit, not a workaround) — verify degrees-vs-radians units against the exact SDK version pinned.
- Gauge charts' rounded arc caps (`SKStrokeCap.Round`) → `ICanvas.StrokeLineCap = LineCap.Round` (direct 1:1).
- `ChartEntry`/`ChartSerie`'s `SKColor` properties → `Microsoft.Maui.Graphics.Color`; audit each remaining `SKColor.Empty` "unset" sentinel use (most fallback chains already use nullable `SKColor?`) and convert to nullable `Color?` where needed.

**Native iOS/Droid platform views:** since `GraphicsView` is a MAUI-controls concept, `Microcharts.iOS`/`Microcharts.Droid` drive Maui.Graphics' platform canvas types directly instead: a `UIView` subclass overriding `DrawRect(CGRect)` that wraps `UIGraphics.GetCurrentContext()` in a Maui.Graphics platform canvas and calls `chart.Draw(canvas, dirtyRect)`; an Android `View` subclass overriding `OnDraw(Canvas)` the same way. This is a less-traveled integration path (most Maui.Graphics platform-canvas usage goes through `GraphicsViewHandler`) — treat it as a spike early (Milestone 0/6), with a fallback of a minimal hand-rolled wrapper if the direct approach proves unworkable, while still avoiding a hard `Microsoft.Maui.Controls` dependency in these two packages. Preserve each `ChartView`'s existing constructors and invalidation plumbing (`Microcharts.iOS/ChartView.cs`, `Microcharts.Droid/ChartView.cs`) verbatim — only the base type and paint-callback body change. While touching Droid, fix the existing asymmetry where its null-chart case doesn't clear to transparent (unlike iOS/Maui).

**Microcharts.Maui:** `ChartView.cs` becomes `class ChartView : GraphicsView, IDrawable` (`Drawable = this`; `Draw(ICanvas, RectF)` mirrors today's `OnPaintCanvas`; `Invalidate()` replaces `InvalidateSurface()`). `AppHostBuilderExtensions.cs`'s `UseMicrocharts()` (currently a passthrough to SkiaSharp's `UseSkiaSharp()`) likely becomes a no-op kept for source compatibility — verify whether `GraphicsView`'s handler needs explicit registration.

## Package/dependency changes

- `Sources/Microcharts/Microcharts.csproj`: remove `PackageReference SkiaSharp`, add `PackageReference Microsoft.Maui.Graphics` (pin an exact version early and write geometry code against its actual API, since `PathF` method names have shifted across MAUI SDK versions).
- `Sources/Microcharts.Maui/Microcharts.Maui.csproj`: drop `SkiaSharp.Views.Maui.Controls`.
- `Sources/Microcharts.iOS/Microcharts.iOS.csproj`, `Sources/Microcharts.Droid/Microcharts.Droid.csproj`: drop `SkiaSharp.Views`, add `Microsoft.Maui.Graphics`.
- `Sources/Microcharts.Metapackage/Microcharts.Metapackage.nuspec`: remove every `SkiaSharp*` `<dependency>` across all 5 TFM groups, replace with `Microsoft.Maui.Graphics` pinned to the same version.
- `Sources/Directory.Build.props`: bump `VersionMain` to `3.0.0`.
- `Sources/Microcharts.Samples/Data.cs` (1686 lines): mechanical pass converting `SKColor.Parse(...)`/`SKColors.*` to `Microsoft.Maui.Graphics.Color` equivalents.
- Sample app `.csproj` files (`Samples.Maui`, `Samples.Android`, `Samples.iOS`): drop their transitive `SkiaSharp.Views*` references once `ChartView` no longer derives from `SKCanvasView`.
- `.github/workflows/pull-request.yml`/`publish.yml`, `buildpackages-maui.sh`, `clean.sh`: no structural changes expected — re-verify after the fact rather than editing preemptively. Preserve the existing nuget.org-exclusion behavior for `Microcharts.iOS.*`/`Microcharts.Droid.*` (pre-existing, unrelated to this rewrite).

## Phased rollout

1. **Scaffolding & text-metrics spike**: add `Microsoft.Maui.Graphics` alongside SkiaSharp (transitional coexistence), confirm it builds across all TFMs. Build `ITextMetricsProvider` + portable implementation as additive code; measure its delta against `SKFont.MeasureText` on representative strings before touching any chart. Pin the exact Maui.Graphics package version.
2. **Core extensions & helpers**: rewrite `CanvasExtensions.cs`, `DrawHelper.cs`, `MeasureHelper.cs` fully against `ICanvas`/`PathF`/`Color`/`ITextMetricsProvider`; port `Chart.cs`'s signature and background-clear; retype `ChartEntry`/`ChartSerie` colors. Nothing runnable yet, but this is the highest-leverage milestone.
3. **Simple/non-gradient charts**: `SimpleChart`, `SeriesChart`, `AxisBasedChart`, `BarChart`. Done when a bar chart renders correctly in one sample screen.
4. **Gradient-heavy charts**: `PointChart` (direct port), `LineChart` (X-gradient direct port + Y-fade documented simplification). Done when both `EnableYFadeOutGradient` states are visually checked against before/after screenshots.
5. **Path/geometry-heavy charts**: `DonutChart`/`RadialHelpers` (EvenOdd `FillPath`, including the full-circle special case), `HalfRadialGaugeChart`/`RadialGaugeChart`. Done when these match pre-rewrite screenshots, including a 100%-single-entry donut test.
6. **RadarChart** (last, highest cumulative complexity — layers dash pattern, rotation trig, and clip on top of Milestone 2's extensions). Done when multi-entry, varying-label-alignment, dashed-ring renders match.
7. **Legacy removal & platform ChartViews**: delete the three `Legacy*` classes; rebuild `Microcharts.Maui`/`Microcharts.iOS`/`Microcharts.Droid` `ChartView`s (including the native-canvas spike and the Droid transparent-clear fix). Done when all three sample apps build and render every chart type from `Data.cs` without crashing.
8. **Samples & packaging**: `Data.cs` color conversion, version bump, nuspec cleanup, CI re-verification. Done when the full solution builds clean on both `windows-latest` and `macos-26`, `dotnet list package --include-transitive` shows zero SkiaSharp references anywhere in the dependency graph, and a manual visual pass across every chart type on every sample app matches the pre-rewrite baseline.

## Known, documented behavior change

`LineChart.EnableYFadeOutGradient = true` combined with multi-entry-color gradients loses the true 2D (X-color × Y-alpha) blend and ships a single-axis alpha approximation instead, since Maui.Graphics has no shader-composition equivalent to `SKShader.CreateCompose(..., SrcOut)`. The feature defaults to `false`, limiting blast radius. Call this out in the 3.0 release notes.

## Verification

The repo has **no test projects anywhere** — validation is build success + manual/screenshot visual comparison against the three sample apps at each milestone (this is a standing gap worth flagging to the user as a good follow-up, separate from this rewrite: a lightweight visual-regression harness rendering each chart type to PNG and diffing against a baseline).

- After each milestone: `dotnet build Sources/Microcharts.slnx --configuration Release` must succeed.
- After Milestone 8: run all three sample apps (`Microcharts.Samples.Maui`, `.Android`, `.iOS`) and visually compare every chart type in `Microcharts.Samples/Data.cs` (bar, donut, pie, line, point, radar, half/full radial gauge) against pre-rewrite screenshots, specifically checking label positioning/centering (the text-metrics risk area) and the two `LineChart` gradient modes.
- Confirm zero SkiaSharp references remain: `dotnet list package --include-transitive` on every project should show no `SkiaSharp*` package anywhere in the graph.
- Confirm CI still passes on both OSes in `pull-request.yml`'s matrix (`windows-latest`, `macos-26`) before merging to `main`.

## Risk register (priority order)

1. **Text-metrics fidelity** — no portable ink-bounds/ascent-descent API in Maui.Graphics; nearly every layout calculation depends on it today. Mitigated by the tiered `ITextMetricsProvider` (platform-accurate on iOS/Android, portable approximation elsewhere), validated numerically before Milestone 2 closes.
2. **`LineChart` Y-fade gradient fidelity** — accepted as a documented, default-off visual simplification.
3. **Native iOS/Android Maui.Graphics platform-canvas integration outside full MAUI hosting** — less-traveled path; front-load as an explicit spike rather than discovering problems late in Milestone 7.
4. **`PathF`/`WindingMode` API surface differences across MAUI SDK versions** — pin an exact version early and verify method names/angle units against that version specifically, not general documentation.
5. **No automated visual-regression safety net** — mitigated informally via the per-milestone screenshot checklist; recommend a follow-up snapshot-test project as separate, future work.
