using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using comic_website.Models;

namespace comic_website.Controllers
{
    public class StoryController : Controller
    {
        private readonly WebDocTruyenEntities db =
            new WebDocTruyenEntities();

        // Danh sách truyện
        public ActionResult Index(string category, string ranking, string nameSort, bool follow = false)
        {
            IQueryable<Story> stories = db.Stories.Include(s => s.Category);

            if (!String.IsNullOrWhiteSpace(category) && category != "Thể Loại")
            {
                stories = stories.Where(s => s.Category.CategoryName == category);
            }

            if (!String.IsNullOrWhiteSpace(ranking))
            {
                if (ranking == "Mới cập nhật")
                {
                    stories = stories.OrderByDescending(s => s.CreatedAt);
                }
                else
                {
                    stories = stories.OrderByDescending(s => s.StoryId);
                }
            }

            if (nameSort == "A-Z")
            {
                stories = stories.OrderBy(s => s.Title);
            }
            else if (nameSort == "Z-A")
            {
                stories = stories.OrderByDescending(s => s.Title);
            }

            ViewBag.SelectedCategory = category;
            ViewBag.SelectedRanking = ranking;
            ViewBag.SelectedNameSort = nameSort;
            ViewBag.FollowOnly = follow;
            ViewBag.Categories = db.Categories
                .OrderBy(c => c.CategoryName)
                .Select(c => c.CategoryName)
                .ToList();
            return View(stories.ToList());
        }

        public ActionResult History()
        {
            return RedirectToAction("Index");
        }

        public ActionResult Search(string q)
        {
            IQueryable<Story> stories = db.Stories.Include(s => s.Category);

            if (!String.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                stories = stories.Where(s => s.Title.Contains(q) || s.Author.Contains(q));
            }

            ViewBag.SearchTerm = q;
            return View("Index", stories.ToList());
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
                if (!db.Categories.Any(c => c.CategoryId == story.CategoryId))
                {
                    ModelState.AddModelError("CategoryId", "Danh mục truyện không hợp lệ.");
                }
                else
                {
                    story.CreatedAt = DateTime.Now;
                    db.Stories.Add(story);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "CategoryName", story.CategoryId);
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
                if (!db.Categories.Any(c => c.CategoryId == story.CategoryId))
                {
                    ModelState.AddModelError("CategoryId", "Danh mục truyện không hợp lệ.");
                }
                else
                {
                    var existingStory = db.Stories.Find(story.StoryId);
                    if (existingStory == null)
                    {
                        return HttpNotFound();
                    }

                    existingStory.CategoryId = story.CategoryId;
                    existingStory.Title = story.Title;
                    existingStory.Author = story.Author;
                    existingStory.Description = story.Description;

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "CategoryName", story.CategoryId);
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
            if (story == null)
            {
                return HttpNotFound();
            }

            db.Stories.Remove(story);
            db.SaveChanges();

            return RedirectToAction("Index");
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