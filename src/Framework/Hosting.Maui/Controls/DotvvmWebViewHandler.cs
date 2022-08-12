using System;
using System.Linq;
using System.Text;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.Maui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotVVM.Framework.Hosting.Maui.Controls
{
    public partial class DotvvmWebViewHandler
    {
        /// <summary>
        /// This field is part of MAUI infrastructure and is not intended for use by application code.
        /// </summary>
        public static readonly PropertyMapper<IDotvvmWebView, DotvvmWebViewHandler> DotvvmWebViewMapper = new(ViewMapper) {
            [nameof(IDotvvmWebView.ExternalNavigationStarting)] = MapNotifyExternalNavigationStarting,
            [nameof(IDotvvmWebView.RouteName)] = MapRouteName,
            [nameof(IDotvvmWebView.Url)] = MapUrl
        };

        /// <summary>
        /// Initializes a new instance of <see cref="DotvvmWebViewHandler"/> with default mappings.
        /// </summary>
        public DotvvmWebViewHandler() : this(DotvvmWebViewMapper)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DotvvmWebViewHandler"/> using the specified mappings.
        /// </summary>
        /// <param name="mapper">The property mappings.</param>
        public DotvvmWebViewHandler(PropertyMapper? mapper) : base(mapper ?? DotvvmWebViewMapper)
        {
        }


        /// <summary>
        /// Maps the <see cref="IDotvvmWebView.HostPage"/> property to the specified handler.
        /// </summary>
        /// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
        /// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
        public static void MapRouteName(DotvvmWebViewHandler handler, IDotvvmWebView webView)
        {
            handler.RouteName = webView.RouteName;
            handler.StartWebViewCoreIfPossible();
        }


        /// <summary>
        /// Maps the <see cref="IDotvvmWebView.HostPage"/> property to the specified handler.
        /// </summary>
        /// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
        /// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
        public static void MapUrl(DotvvmWebViewHandler handler, IDotvvmWebView webView)
        {
            handler.Url = webView.Url;
            handler.StartWebViewCoreIfPossible();
        }

        /// <summary>
        /// Maps the <see cref="DotvvmWebView.NotifyExternalNavigationStarting"/> property to the specified handler.
        /// </summary>
        /// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
        /// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
        public static void MapNotifyExternalNavigationStarting(DotvvmWebViewHandler handler, IDotvvmWebView webView)
        {
            if (webView is DotvvmWebView dotvvmWebView)
            {
                handler.ExternalNavigationStarting = dotvvmWebView.NotifyExternalNavigationStarting;
            }
        }

        private string routeName;
        public string RouteName
        {
            get
            {
                return routeName;
            }
            set
            {
                if (!string.IsNullOrEmpty(value) && value != routeName)
                {
                    NavigateToRoute(value);
                }
            }
        }

        protected void NavigateToRoute(string value)
        {
            // make sure DotVVM is initialized
            _ = Services.GetRequiredService<DotvvmWebRequestHandler>();

            var route = Services.GetRequiredService<DotvvmConfiguration>().RouteTable[value];
            var url = route.BuildUrl().TrimStart('~');

            routeName = value;
            Url = url;
        }

        public string Url
        {
            get
            {
                return PlatformView.Source?.ToString();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _webviewManager?.Navigate(value);
                }
            }
        }

        partial void StartWebViewCoreIfPossible();

        internal Action<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;
    }
}
