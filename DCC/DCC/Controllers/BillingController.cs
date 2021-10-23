using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DCC.Controllers
{
    public class BillingController : DCCBaseController
    {
        [Authorize]
        // GET: Billing
        public ActionResult Index()
        {
            EmptyView r = new EmptyView();
            setViewModelBase((ViewModelBase)r);

            return View(r);
        }
    }
}