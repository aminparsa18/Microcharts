namespace Microcharts.Droid
{
    using System;
    using Android.Content;
    using Android.Graphics;
    using Android.Runtime;
    using Android.Util;
    using Android.Views;
    using Microsoft.Maui.Graphics.Platform;
    using MauiRectF = Microsoft.Maui.Graphics.RectF;

    public class ChartView : View
    {
        #region Constructors

        public ChartView(Context context) : base(context)
        {
            Initialize();
        }

        public ChartView(Context context, IAttributeSet attributes) : base(context, attributes)
        {
            Initialize();
        }

        public ChartView(Context context, IAttributeSet attributes, int defStyleAtt) : base(context, attributes, defStyleAtt)
        {
            Initialize();
        }

        public ChartView(IntPtr ptr, JniHandleOwnership jni) : base(ptr, jni)
        {
            Initialize();
        }

        private void Initialize()
        {
            // A plain View skips OnDraw entirely unless told otherwise, since it assumes a background-less
            // View (no Drawable background set here) has nothing to paint.
            SetWillNotDraw(false);
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

        /// <remarks>
        /// This is a pure-native (non-MAUI-controls) package, so there is no <c>GraphicsView</c> handler to
        /// draw through -- <c>Microsoft.Maui.Graphics.Platform.PlatformCanvas</c> (the same type MAUI's own
        /// handler uses internally) is driven directly off the <see cref="Canvas"/> <c>OnDraw</c> hands us.
        ///
        /// Android's <see cref="Canvas"/> here is already pixel-space (View.Width/Height are pixels), matching
        /// the pre-migration SkiaSharp-based <c>SKCanvasView</c>'s behavior (it did not set
        /// <c>IgnorePixelScaling</c>, so it drew in device pixels too). <see cref="PlatformCanvas"/>'s
        /// constructor otherwise auto-sets <c>DisplayScale</c> from the device density, which would rescale
        /// every draw call -- that's overridden back to 1 to preserve exact pixel-space parity with the old
        /// rendering, the same way the iOS port compensates for the opposite (point-space) default.
        ///
        /// Also fixes the pre-migration asymmetry where a null <see cref="Chart"/> left stale content on
        /// screen here (unlike the iOS/MAUI views, which always clear) -- explicitly clears to transparent.
        /// </remarks>
        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (this.chart == null)
            {
                canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
                return;
            }

            using (var platformCanvas = new PlatformCanvas(Context) { Canvas = canvas, DisplayScale = 1f })
            {
                this.chart.Draw(platformCanvas, new MauiRectF(0, 0, this.Width, this.Height));
            }
        }

        #endregion
    }
}
