using Comic_web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Comic_web.Controllers
{
    public class CommentController : Controller
    {
        private WebDocTruyenEntities db =
    new WebDocTruyenEntities();
        // GET: Comment
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Add(Comment comment)
        {
            db.Comments.Add(comment);

            db.SaveChanges();

            return RedirectToAction(
                "Details",
                "Story",
                new { id = comment.StoryId });
        }
    }
}