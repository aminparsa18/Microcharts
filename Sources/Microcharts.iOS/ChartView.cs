using System;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using UIKit;

namespace Microcharts.iOS
{
    [Register("ChartView")]
    public class ChartView : UIView
    {
        #region Constructors

        public ChartView()
        {
            Initialize();
        }

        [Preserve]
        public ChartView(IntPtr handle) : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            Initialize();
        }

        private void Initialize()
        {
            this.BackgroundColor = UIColor.Clear;
            this.Opaque = false;
        }

        #endregion

        #region Fields

        private InvalidatedWeakEventHandler<ChartView> handler;

        private Chart chart;

        #endregion

        #region Properties

        public Chart Chart
        {
            get => this.chart;
            set
            {
                if (this.chart != value)
                {
                    if (this.chart != null)
                    {
                        handler.Dispose();
                        this.handler = null;
                    }

                    this.chart = value;
                    // Marshal the initial invalidate too, in case Chart is assigned off the UI thread.
                    this.BeginInvokeOnMainThread(this.InvalidateChart);

                    if (this.chart != null)
                    {
                        // Marshal to the main thread: chart properties may be changed off the UI thread.
                        this.handler = this.chart.ObserveInvalidate(this, (view) => view.BeginInvokeOnMainThread(view.InvalidateChart));
                    }
                }
            }
        }

        #endregion

        #region Methods

        private void InvalidateChart() => this.SetNeedsDisplayInRect(this.Bounds);

        /// <remarks>
        /// This is a pure-native (non-MAUI-controls) package, so there is no <c>GraphicsView</c> handler to
        /// draw through -- <c>Microsoft.Maui.Graphics.Platform.PlatformCanvas</c> (the same type MAUI's own
        /// handler uses internally) is driven directly off the <see cref="CGContext"/> <c>DrawRect</c> hands us.
        ///
        /// <c>DrawRect</c>'s <see cref="CGContext"/> is in points, not device pixels (UIKit rasterizes the
        /// backing store at the screen's scale factor transparently). The pre-migration SkiaSharp-based
        /// <c>SKCanvasView</c> here did not set <c>IgnorePixelScaling</c>, so it drew (and sized Margin,
        /// LineSize, LabelTextSize, etc.) directly in device pixels -- to preserve that exact visual output
        /// (rather than silently resizing every chart by the screen's scale factor), the context is scaled by
        /// <c>1/scale</c> so that one Microcharts drawing unit still maps to one physical pixel, and
        /// <see cref="Chart.Draw"/> is handed the pixel, not point, dimensions.
        /// </remarks>
        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            if (this.chart == null)
            {
                return;
            }

            var context = UIGraphics.GetCurrentContext();
            if (context == null)
            {
                return;
            }

            var scale = (float)(this.ContentScaleFactor > 0 ? this.ContentScaleFactor : 1f);
            var pixelWidth = (float)(this.Bounds.Width * scale);
            var pixelHeight = (float)(this.Bounds.Height * scale);

            context.SaveState();
            context.ScaleCTM((nfloat)(1f / scale), (nfloat)(1f / scale));

            using (var canvas = new PlatformCanvas(() => CGColorSpace.CreateDeviceRGB()) { Context = context })
            {
                this.chart.Draw(canvas, new RectF(0, 0, pixelWidth, pixelHeight));
            }

            context.RestoreState();
        }

        #endregion
    }
}
