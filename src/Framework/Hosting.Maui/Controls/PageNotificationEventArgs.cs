namespace DotVVM.Framework.Hosting.Maui.Controls;

public class PageNotificationEventArgs
{
    public string MethodName { get; }

    public object[] Arguments { get; }
    
    public PageNotificationEventArgs(string methodName, object[] args)
    {
        MethodName = methodName;
        Arguments = args;
    }
}
