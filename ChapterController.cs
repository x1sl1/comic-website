using System.Linq;
using System.Web.Mvc;
using Comic_web.Models;

namespace Comic_web.Controllers
{
    public class ChapterController : Controller
    {
        private WebDocTruyenEntities db = new WebDocTruyenEntities();

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
    }
}