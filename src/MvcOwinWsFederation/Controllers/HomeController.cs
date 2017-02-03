using System.Web;
using System.Web.Mvc;

namespace MvcOwinWsFederation.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult Claims()
        {
            ViewBag.Message = "Claims";

            return View();
        }

        public ActionResult SignOut()
        {
            Request.GetOwinContext().Authentication.SignOut();
            return View();
        }

        public ActionResult SignOutCleanup()
        {
            // this should only signout of cookies since it will be running in an iframe
            Request.GetOwinContext().Authentication.SignOut("Cookies");
            return new EmptyResult();
        }
    }
}