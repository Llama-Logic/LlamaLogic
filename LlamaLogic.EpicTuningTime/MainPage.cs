namespace LlamaLogic.EpicTuningTime;

sealed class MainPage :
    ContentPage
{
    public MainPage()
    {
        var blazorWebView = new BlazorWebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            HostPage = "wwwroot/index.html",
            VerticalOptions = LayoutOptions.Fill
        };
        blazorWebView.RootComponents.Add(new RootComponent
        {
            ComponentType = typeof(Routes),
            Selector = "#app"
        });
        Content = blazorWebView;
    }
}