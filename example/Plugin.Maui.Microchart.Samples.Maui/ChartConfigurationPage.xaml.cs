using Plugin.Maui.Microchart.Samples.Core;

namespace Plugin.Maui.Microchart.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChartConfigurationPage
    {
        public ChartConfigurationPage(string chartType)
        {
            Items = Data.CreateXamarinExampleChartItem(chartType).ToList();
            InitializeComponent();
            Title = chartType;
        }

        public List<ExampleChartItem> Items { get; }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            var border = sender as Border;
            ExampleChartItem exChartItem = border.BindingContext as ExampleChartItem;
            Navigation.PushAsync(new ChartPage(exChartItem));
        }
    }
}
