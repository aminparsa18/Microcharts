namespace Microcharts.Maui
{
    using Microsoft.Maui.Controls;
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

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (this.chart != null)
            {
                this.chart.Draw(canvas, dirtyRect);
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
