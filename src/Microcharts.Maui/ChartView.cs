namespace Microcharts.Maui
{
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Devices;
    using Microsoft.Maui.Graphics;

    public class ChartView : GraphicsView, IDrawable
    {
        #region Constructors

        public ChartView()
        {
            this.Drawable = this;
            this.BackgroundColor = Colors.Transparent;
        }

        #endregion

        #region Static fields

        public static readonly BindableProperty ChartProperty = BindableProperty.Create(nameof(Chart), typeof(Chart), typeof(ChartView), null, propertyChanged: OnChartChanged);

        #endregion

        #region Fields

        private InvalidatedWeakEventHandler<ChartView> handler;

        private Chart chart;

        #endregion

        #region Properties

        public Chart Chart
        {
            get { return (Chart)GetValue(ChartProperty); }
            set { SetValue(ChartProperty, value); }
        }

        #endregion

        #region Methods

        private static void OnChartChanged(BindableObject d, object oldValue, object value)
        {
            var view = d as ChartView;

            if (view.chart != null)
            {
                view.handler.Dispose();
                view.handler = null;
            }

            view.chart = value as Chart;
            view.Invalidate();

            if (view.chart != null)
            {
                view.handler = view.chart.ObserveInvalidate(view, (v) => v.Dispatcher.Dispatch(v.Invalidate));
            }
        }

        /// <remarks>
        /// Unlike <c>Microcharts.iOS</c>/<c>Microcharts.Droid</c> (pure-native, driving
        /// <c>Microsoft.Maui.Graphics.Platform.PlatformCanvas</c> directly against a pixel-space platform
        /// canvas -- see those two <c>ChartView</c>s for the matching compensation), <see cref="GraphicsView"/>
        /// hands <see cref="Draw"/> a device-independent (DIP) <paramref name="dirtyRect"/>, roughly 2-3x
        /// smaller per dimension than the device-pixel canvas the pre-migration SkiaSharp <c>SKCanvasView</c>
        /// drew into (it never set <c>IgnorePixelScaling</c>). The chart's absolute sizing constants
        /// (<c>Margin</c>, <c>LabelTextSize</c>, etc.) were tuned for that pixel-space canvas, so left alone
        /// here they'd consume a much larger fraction of the (smaller) DIP canvas -- oversized text, squeezed
        /// bars. Compensate the same way the native views do: hand the chart a pixel-sized rect, and scale the
        /// canvas down so the pixel-space drawing still lands within the DIP-sized view.
        /// </remarks>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (this.chart != null)
            {
                float density = 1f;
                try
                {
                    density = (float)DeviceDisplay.Current.MainDisplayInfo.Density;
                }
                catch
                {
                    // DeviceDisplay isn't implemented on every TFM this package targets (e.g. bare net10.0,
                    // used for compile/pack purposes only) -- fall back to no scaling rather than throw.
                }

                if (density > 0 && density != 1f)
                {
                    canvas.SaveState();
                    canvas.Scale(1f / density, 1f / density);
                    this.chart.Draw(canvas, new RectF(0, 0, dirtyRect.Width * density, dirtyRect.Height * density));
                    canvas.RestoreState();
                }
                else
                {
                    this.chart.Draw(canvas, dirtyRect);
                }
            }
            else
            {
                canvas.FillColor = Colors.Transparent;
                canvas.FillRectangle(dirtyRect);
            }
        }

        #endregion
    }
}
