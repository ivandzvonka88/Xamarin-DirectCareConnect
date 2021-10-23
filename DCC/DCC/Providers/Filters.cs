using System;

using System.Web.Mvc;
using System.Web.Helpers;

using System.Web.Http.Controllers;

namespace DCC
{


    public class AJAXAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.RequestContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.Result = new HttpStatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);
                System.Web.HttpContext.Current.Response.Write("Not Logged In. Login timeout expired");
            }
            else
                base.HandleUnauthorizedRequest(filterContext);
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ValidateJsonAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            var httpContext = filterContext.HttpContext;
            var cookie = httpContext.Request.Cookies[AntiForgeryConfig.CookieName];
            AntiForgery.Validate(cookie != null ? cookie.Value : null, httpContext.Request.Headers["__RequestVerificationToken"]);
        }
    }

    public class ValidateAntiForgeryHeader : FilterAttribute, IAuthorizationFilter
    {
        private const string KEY_NAME = "__RequestVerificationToken";

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            string clientToken = filterContext.RequestContext.HttpContext.Request.Headers.Get(KEY_NAME);
            if (clientToken == null) throw new HttpAntiForgeryException(String.Format("Header does not contain {0}", KEY_NAME));

            string serverToken = filterContext.HttpContext.Request.Cookies.Get(KEY_NAME).Value;
            if (serverToken == null) throw new HttpAntiForgeryException(String.Format("Cookies does not contain {0}", KEY_NAME));

            AntiForgery.Validate(serverToken, clientToken);
        }
    }
}