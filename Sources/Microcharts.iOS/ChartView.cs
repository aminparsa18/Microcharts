using System;
using Foundation;
using SkiaSharp;
using UIKit;
using SkiaSharp.Views.iOS;
using System.Diagnostics;

namespace Microcharts.iOS
{
    [Register("ChartView")]
    public class ChartView : SKCanvasView
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
            this.PaintSurface += OnPaintCanvas;
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

        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e)
        {
            if (this.chart != null)
            {
                this.chart.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
            }
            else
            {
                e.Surface.Canvas.Clear(SKColors.Transparent);
            }
        }

        #endregion
    }
}
