namespace DotVVM.Framework.Hosting.Maui.Services.Messaging;

public class InitCompletedMessage
{
    public string RouteName { get; set; }

    public Dictionary<string, string> RouteParameters { get; set; }
}
