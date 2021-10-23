using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Helpers;
using System.Security.Claims;
using System.Configuration;
using Newtonsoft.Json;
namespace DCC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

        }
        
        private void Application_BeginRequest(Object source, EventArgs e)
        {
            if (!HttpContext.Current.Request.IsSecureConnection && ConfigurationManager.AppSettings["SSL"] == "1")
                HttpContext.Current.Response.Redirect(HttpContext.Current.Request.Url.ToString().Replace("http","https"));
        }

        void Application_Error(object sender, EventArgs e)
        {
            Exception exc = Server.GetLastError();

            //if (exc is HttpUnhandledException)
            //{
            //    // Pass the error on to the error page.
            //    Server.Transfer("ErrorPage.aspx?handler=Application_Error%20-%20Global.asax", true);
            //}
        }

    }
}
