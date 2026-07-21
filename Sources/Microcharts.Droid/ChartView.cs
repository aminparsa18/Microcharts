namespace Microcharts.Droid
{
    using Android.Content;
    using SkiaSharp.Views.Android;
    using Android.Util;
    using System;
    using Android.Runtime;

    public class ChartView : SKCanvasView
    {
        #region Constructors

        public ChartView(Context context) : base(context)
        {
            this.PaintSurface += OnPaintCanvas;
        }

        public ChartView(Context context, IAttributeSet attributes) : base(context, attributes)
        {
            this.PaintSurface += OnPaintCanvas;
        }

        public ChartView(Context context, IAttributeSet attributes, int defStyleAtt) : base(context, attributes, defStyleAtt)
        {
            this.PaintSurface += OnPaintCanvas;
        }

        public ChartView(IntPtr ptr, JniHandleOwnership jni) : base(ptr, jni)
        {
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
                    this.PostInvalidate();

                    if (this.chart != null)
                    {
                        // PostInvalidate is safe from any thread: chart properties may be changed off the UI thread.
                        this.handler = this.chart.ObserveInvalidate(this, (view) => { try { view.PostInvalidate(); } catch (ObjectDisposedException) { } });
                    }
                }
            }
        }

        #endregion

        #region Methods

        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e)
        {
            if (this.chart != null)
            {
                this.chart.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
            }
        }

        #endregion
    }
}
