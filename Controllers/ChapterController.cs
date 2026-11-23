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

            ViewBag.PreviousChapterId = db.Chapters
                .Where(item => item.StoryId == chapter.StoryId && item.ChapterNumber < chapter.ChapterNumber)
                .OrderByDescending(item => item.ChapterNumber)
                .Select(item => (int?)item.ChapterId)
                .FirstOrDefault();
            ViewBag.NextChapterId = db.Chapters
                .Where(item => item.StoryId == chapter.StoryId && item.ChapterNumber > chapter.ChapterNumber)
                .OrderBy(item => item.ChapterNumber)
                .Select(item => (int?)item.ChapterId)
                .FirstOrDefault();

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
