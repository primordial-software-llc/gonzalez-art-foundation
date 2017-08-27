using System.Web.Mvc;

namespace MVC5App.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Slideshow()
        {
            ViewBag.Message = "Gallery slideshow.";

            return View();
        }
    }
}