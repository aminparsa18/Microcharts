using Plugin.Maui.Microchart.Samples.Core;
using Plugin.Maui.Microchart.Samples.Model;

namespace Plugin.Maui.Microchart.Samples
{
    public partial class MainPage
    {
        public MainPage()
        {
            var charts = Data.CreateXamarinSample();
            var items = new List<ChartItem>();
            for (int i = 0; i < charts.Length; i++)
            {
                items.Add(new ChartItem(charts[i].GetType().Name, charts[i], i));
            }
            Items = items;
            InitializeComponent();
        }

        public List<ChartItem> Items { get; }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            var border = sender as Border;
            ChartItem chartItem = border.BindingContext as ChartItem;
            Navigation.PushAsync(new ChartConfigurationPage(chartItem.Name));
        }
    }
}
