using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using GameStore.WebUI.Controllers;
using System.Web.Mvc;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.AspNet.Identity.EntityFramework;

namespace GameStore.WebUI.Controllers
{
    public class CommonController : BaseController
    {
        // GET: Common
        public ActionResult About(int code = 0)
        {
            ViewBag.Title = "About";
            ViewBag.code = code;
            return View();
        }

        public ActionResult ExchangePolicy(int code = 0)
        {
            ViewBag.Title = "Exchange Policy";
            ViewBag.code = code;
            return View();
        }
    }
}