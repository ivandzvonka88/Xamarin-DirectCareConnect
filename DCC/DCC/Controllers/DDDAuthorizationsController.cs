using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using DCC.Models;
using System.IO;

namespace DCC.Controllers
{
    public class DDDAuthorizationsController : DCCBaseController
    {
        public class Creds
        {
            public string un { get; set; }
            public string pw { get; set; }
        }

        [Authorize]
        public ActionResult Index()
        {
            ViewModelBase r = new EmptyView();

            setViewModelBase((ViewModelBase)r);

            return View("Index", r);
        }



        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public ActionResult UploadAuth(IEnumerable<HttpPostedFileBase> files)
        {
            Er er = new Er();
            if (files != null)
            {
                var file = files.FirstOrDefault();
                // Verify that the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    Stream strm = file.InputStream;

                    AuthorizationsProcessor AuthProcessor = new AuthorizationsProcessor();
                    try
                    {
                        AuthProcessor.processAuthFile(strm, UserClaim.conStr, ref er);
                    }
                    catch (Exception ex)
                    {
                        er.code = 1;
                        er.msg = ex.Message;
                    }
                    if (strm != null)
                        strm.Dispose();

                }

            }
            return Json(er);
        }

      
        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public ActionResult GetAuthFromFocus(Creds c)
        {
            Er er = new Er();
            FocusDownloader FocusDownloader = new FocusDownloader(new CookieContainer());
            AuthorizationsProcessor AuthProcessor = new AuthorizationsProcessor();
            try
            {
                FocusDownloader.startUpload(c.un, c.pw, UserClaim.conStr, ref er);
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            FocusDownloader.Dispose();
            return Json(er);
        }



    }
}