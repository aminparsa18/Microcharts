using Microsoft.Maui.Hosting;

namespace Plugin.Maui.Microchart
{
    public static class AppHostBuilderExtensions
    {
        /// <summary>
        /// Kept for source compatibility with existing app startup code. <see cref="ChartView"/> is now a
        /// plain <c>GraphicsView</c>, whose handler is already registered by MAUI's default handler
        /// registration -- unlike the SkiaSharp-backed <c>SKCanvasView</c> it replaced, no extra
        /// <c>UseSkiaSharp()</c>-style call-out is needed, so this is a no-op passthrough.
        /// </summary>
        public static MauiAppBuilder UseMicrocharts(this MauiAppBuilder builder) => builder;
    }
}
