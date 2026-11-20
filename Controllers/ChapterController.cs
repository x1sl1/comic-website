using System.Linq;
using System.Web.Mvc;
using comic_website.Models;

namespace comic_website.Controllers
{
    public class ChapterController : Controller
    {
        private readonly WebDocTruyenEntities db = new WebDocTruyenEntities();

        public ActionResult Read(int id)
        {
            var chapter = db.Chapters
                            .FirstOrDefault(c => c.ChapterId == id);

            if (chapter == null)
            {
                return HttpNotFound();
            }

            return View(chapter);
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