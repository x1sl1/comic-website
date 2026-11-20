using System.Linq;
using System.Web.Mvc;
using comic_website.Models;

namespace comic_website.Controllers
{
    public class HomeController : Controller
    {
        private readonly WebDocTruyenEntities db = new WebDocTruyenEntities();

        public ActionResult Index()
        {
            var stories = db.Stories
                .Include("Chapters")
                .OrderByDescending(s => s.CreatedAt)
                .Take(21)
                .ToList();

            return View(stories);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}