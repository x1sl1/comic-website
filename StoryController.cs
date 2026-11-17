using Comic_web.Models;
using System.Data.Entity;
using System.Web.Mvc;
using System.Linq;
namespace Comic_web.Controllers
{
    public class StoryController : Controller
    {
        private WebDocTruyenEntities db =
            new WebDocTruyenEntities();

        // Danh sách truyện
        public ActionResult Index()
        {
            var stories = db.Stories
                            .Include(s => s.Category)
                            .ToList();

            return View(stories);
        }

        // Chi tiết truyện
        public ActionResult Details(int id)
        {
            var story = db.Stories
                          .Include(s => s.Chapters)
                          .Include(s => s.Comments)
                          .FirstOrDefault(s => s.StoryId == id);

            if (story == null)
            {
                return HttpNotFound();
            }

            return View(story);
        }

        // GET: Story/Create
        public ActionResult Create()
        {
            ViewBag.CategoryId =
                new SelectList(db.Categories,
                               "CategoryId",
                               "CategoryName");

            return View();
        }

        // POST: Story/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Story story)
        {
            if (ModelState.IsValid)
            {
                story.CreatedAt = System.DateTime.Now;

                db.Stories.Add(story);

                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(story);
        }

        // GET: Story/Edit/5
        public ActionResult Edit(int id)
        {
            var story = db.Stories.Find(id);

            if (story == null)
            {
                return HttpNotFound();
            }

            ViewBag.CategoryId =
                new SelectList(db.Categories,
                               "CategoryId",
                               "CategoryName",
                               story.CategoryId);

            return View(story);
        }

        // POST: Story/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Story story)
        {
            if (ModelState.IsValid)
            {
                db.Entry(story).State =
                    EntityState.Modified;

                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(story);
        }

        // GET: Story/Delete/5
        public ActionResult Delete(int id)
        {
            var story = db.Stories.Find(id);

            if (story == null)
            {
                return HttpNotFound();
            }

            return View(story);
        }

        // POST: Story/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var story = db.Stories.Find(id);

            db.Stories.Remove(story);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

    }
}