namespace DotVVM.Samples.BasicSamples.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            BindingContext = new MainPageViewModel() { RouteName = "Default" };
        }


    }
}
