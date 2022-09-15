namespace DotVVM.Framework.Hosting.Maui.Services.Messaging;

public class NavigationCompletedMessage
{
    public string RouteName { get; set; }

    public Dictionary<string, string> RouteParameters { get; set; }
}
