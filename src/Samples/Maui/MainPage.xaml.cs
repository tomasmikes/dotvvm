using Newtonsoft.Json;

namespace DotVVM.Samples.BasicSamples.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            BindingContext = new MainPageViewModel() { RouteName = "ControlSamples_Button_Button" };
        }

        private async void GetViewModelStateButton_Clicked(object sender, EventArgs e)
        {
            var viewModelStateSnapshot = await DotvvmPage.GetViewModelSnapshot();
            await DisplayAlert("View Model State", viewModelStateSnapshot, "OK");
        }

        private async void PatchViewModelStateButton_Clicked(object sender, EventArgs e)
        {
            await DotvvmPage.PatchViewModel(new { Count = 10 });
        }

        private async void DotvvmPage_PageNotificationReceived(object sender, Framework.Hosting.Maui.Controls.PageNotificationEventArgs e)
        {
            await DisplayAlert("PageNotification", e.MethodName + "(" + string.Join(", ", e.Arguments.Select(JsonConvert.SerializeObject)) + ")", "OK");
        }
    }
}
