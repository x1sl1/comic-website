using System;
using System.Linq;
using System.Web.Mvc;
using comic_website.Models;

namespace comic_website.Controllers
{
    public class CommentController : Controller
    {
        private readonly WebDocTruyenEntities db =
            new WebDocTruyenEntities();
        // GET: Comment
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(Comment comment)
        {
            if (comment == null || comment.StoryId <= 0 || String.IsNullOrWhiteSpace(comment.Content))
            {
                return new HttpStatusCodeResult(400, "Nội dung bình luận không hợp lệ.");
            }

            if (!db.Stories.Any(s => s.StoryId == comment.StoryId))
            {
                return HttpNotFound();
            }

            if (Session["UserId"] != null)
            {
                comment.UserId = (int)Session["UserId"];
            }

            comment.Content = comment.Content.Trim();
            comment.CreatedAt = DateTime.Now;
            db.Comments.Add(comment);
            db.SaveChanges();

            return RedirectToAction(
                "Details",
                "Story",
                new { id = comment.StoryId });
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