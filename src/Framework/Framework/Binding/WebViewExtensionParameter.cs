using System;
using System.Linq.Expressions;
using DotVVM.Framework.Compilation.ControlTree;
using DotVVM.Framework.Compilation.ControlTree.Resolved;
using DotVVM.Framework.Compilation.Javascript;
using DotVVM.Framework.Compilation.Javascript.Ast;

namespace DotVVM.Framework.Binding;

public class WebViewExtensionParameter : BindingExtensionParameter
{
    public WebViewExtensionParameter()
        : base("_webview", new ResolvedTypeDescriptor(typeof(WebViewBindingApi)), true)
    {
    }

    public override Expression GetServerEquivalent(Expression controlParameter)
    {
        return Expression.New(typeof(WebViewBindingApi));
    }

    public override JsExpression GetJsTranslation(JsExpression dataContext)
    {
        return new JsIdentifierExpression("dotvvm").Member("webView");
    }
}

public class WebViewBindingApi
{
    public void NotifyMauiPage(string methodName, params object[] args) =>
        throw new Exception($"Cannot invoke JS command server-side: {methodName}({string.Join(", ", args)}).");

    internal static void RegisterJavascriptTranslations(JavascriptTranslatableMethodCollection collection)
    {
        collection.AddMethodTranslator(typeof(WebViewBindingApi), nameof(NotifyMauiPage),
            new GenericMethodCompiler(a => new JsInvocationExpression(a[0].Member("notifyMauiPage"), a[1], a[2])));
    }
}
