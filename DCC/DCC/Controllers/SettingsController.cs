using DCC.Models.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DCC.Controllers
{
    public class SettingsController : DCCBaseController
    {
        [Authorize]
        public ActionResult Index()
        {
            //EmptyView r = new EmptyView();
            ProviderInit r = new ProviderInit();
            setViewModelBase((ViewModelBase)r);

            return View(r);
        }
    }
}