using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using comic_website.Models;

namespace comic_website.Controllers
{
    public class AdminController : Controller
    {
        private readonly WebDocTruyenEntities db = new WebDocTruyenEntities();

        public ActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            ViewBag.StoryCount = db.Stories.Count();
            ViewBag.ChapterCount = db.Chapters.Count();
            ViewBag.CommentCount = db.Comments.Count();
            ViewBag.UserCount = db.Users.Count();
            ViewBag.RecentStories = db.Stories
                .OrderByDescending(s => s.CreatedAt)
                .Take(8)
                .ToList();
            return View();
        }

        public ActionResult Stories()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var stories = db.Stories
                .Include("Category")
                .Include("Chapters")
                .OrderByDescending(s => s.CreatedAt)
                .ToList();
            return View(stories);
        }

        public ActionResult CreateStory()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.CategoryName), "CategoryId", "CategoryName");
            return View(new Story());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStory(Story story, HttpPostedFileBase coverFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            if (String.IsNullOrWhiteSpace(story.Title) || story.CategoryId <= 0)
            {
                ModelState.AddModelError("", "Tên truyện và thể loại là bắt buộc.");
            }

            if (!db.Categories.Any(c => c.CategoryId == story.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Thể loại không hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                story.CreatedAt = DateTime.Now;
                db.Stories.Add(story);
                db.SaveChanges();

                SaveCover(story.StoryId, coverFile);
                TempData["Success"] = "Đã thêm truyện mới.";
                return RedirectToAction("Stories");
            }

            ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.CategoryName), "CategoryId", "CategoryName", story.CategoryId);
            return View(story);
        }

        [HttpGet]
        public ActionResult EditStory(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");
            if (!id.HasValue || id.Value <= 0)
            {
                return RedirectToAction("Stories");
            }

            var story = db.Stories.Find(id.Value);
            if (story == null) return HttpNotFound();

            ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.CategoryName), "CategoryId", "CategoryName", story.CategoryId);
            ViewBag.CoverUrl = GetCoverUrl(id.Value);
            return View(story);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStory(Story story, HttpPostedFileBase coverFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var existing = db.Stories.Find(story.StoryId);
            if (existing == null) return HttpNotFound();

            existing.CategoryId = story.CategoryId;
            existing.Title = story.Title;
            existing.Author = story.Author;
            existing.Description = story.Description;

            if (String.IsNullOrWhiteSpace(existing.Title) || existing.CategoryId <= 0)
            {
                ModelState.AddModelError("", "Tên truyện và thể loại là bắt buộc.");
            }

            if (!db.Categories.Any(c => c.CategoryId == existing.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Thể loại không hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                db.SaveChanges();
                SaveCover(existing.StoryId, coverFile);
                TempData["Success"] = "Đã cập nhật thông tin truyện.";
                return RedirectToAction("Stories");
            }

            ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.CategoryName), "CategoryId", "CategoryName", existing.CategoryId);
            ViewBag.CoverUrl = GetCoverUrl(existing.StoryId);
            return View(existing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStory(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var story = db.Stories.Find(id);
            if (story == null) return HttpNotFound();

            db.Stories.Remove(story);
            db.SaveChanges();
            RemoveCover(id);
            TempData["Success"] = "Đã xóa truyện.";
            return RedirectToAction("Stories");
        }

        public ActionResult Chapters(int storyId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var story = db.Stories
                .Include("Chapters")
                .FirstOrDefault(s => s.StoryId == storyId);
            if (story == null) return HttpNotFound();

            ViewBag.Story = story;
            return View(story.Chapters.OrderBy(c => c.ChapterNumber).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateChapter(int storyId, int chapterNumber, string title, HttpPostedFileBase chapterImage)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            if (!db.Stories.Any(s => s.StoryId == storyId)) return HttpNotFound();
            if (chapterNumber < 1)
            {
                TempData["Error"] = "Số chương phải lớn hơn 0.";
                return RedirectToAction("Chapters", new { storyId = storyId });
            }

            if (db.Chapters.Any(c => c.StoryId == storyId && c.ChapterNumber == chapterNumber))
            {
                TempData["Error"] = "Số chương này đã tồn tại.";
                return RedirectToAction("Chapters", new { storyId = storyId });
            }

            string imageUrl;
            string uploadError;
            if (!TrySaveChapterImage(storyId, chapterImage, out imageUrl, out uploadError))
            {
                TempData["Error"] = uploadError;
                return RedirectToAction("Chapters", new { storyId = storyId });
            }

            db.Chapters.Add(new Chapter
            {
                StoryId = storyId,
                ChapterNumber = chapterNumber,
                Title = title,
                Content = imageUrl
            });
            db.SaveChanges();

            TempData["Success"] = "Đã tải lên chương mới.";
            return RedirectToAction("Chapters", new { storyId = storyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditChapter(int chapterId, int chapterNumber, string title, HttpPostedFileBase chapterImage)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var chapter = db.Chapters.Find(chapterId);
            if (chapter == null) return HttpNotFound();

            if (chapterNumber < 1)
            {
                TempData["Error"] = "Số chương phải lớn hơn 0.";
                return RedirectToAction("Chapters", new { storyId = chapter.StoryId });
            }

            var duplicate = db.Chapters.Any(c => c.ChapterId != chapterId && c.StoryId == chapter.StoryId && c.ChapterNumber == chapterNumber);
            if (duplicate)
            {
                TempData["Error"] = "Số chương này đã tồn tại.";
                return RedirectToAction("Chapters", new { storyId = chapter.StoryId });
            }

            var oldImageUrl = chapter.Content;
            var imageUrl = oldImageUrl;
            if (chapterImage != null && chapterImage.ContentLength > 0)
            {
                string uploadError;
                if (!TrySaveChapterImage(chapter.StoryId, chapterImage, out imageUrl, out uploadError))
                {
                    TempData["Error"] = uploadError;
                    return RedirectToAction("Chapters", new { storyId = chapter.StoryId });
                }
            }
            else if (!IsChapterImageUrl(oldImageUrl))
            {
                TempData["Error"] = "Vui lòng chọn ảnh chương.";
                return RedirectToAction("Chapters", new { storyId = chapter.StoryId });
            }

            chapter.ChapterNumber = chapterNumber;
            chapter.Title = title;
            chapter.Content = imageUrl;
            db.SaveChanges();

            if (!String.Equals(oldImageUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
            {
                DeleteChapterImage(oldImageUrl);
            }

            TempData["Success"] = "Đã cập nhật chương.";
            return RedirectToAction("Chapters", new { storyId = chapter.StoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteChapter(int chapterId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var chapter = db.Chapters.Find(chapterId);
            if (chapter == null) return HttpNotFound();

            var storyId = chapter.StoryId;
            var imageUrl = chapter.Content;
            db.Chapters.Remove(chapter);
            db.SaveChanges();
            DeleteChapterImage(imageUrl);

            TempData["Success"] = "Đã xóa chương.";
            return RedirectToAction("Chapters", new { storyId = storyId });
        }

        private bool IsAdmin()
        {
            return Session["IsAdmin"] is bool && (bool)Session["IsAdmin"];
        }

        private void SaveCover(int storyId, HttpPostedFileBase coverFile)
        {
            if (coverFile == null || coverFile.ContentLength <= 0) return;

            var extension = Path.GetExtension(coverFile.FileName);
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(extension == null ? "" : extension.ToLowerInvariant()))
            {
                TempData["Error"] = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.";
                return;
            }

            var uploads = Server.MapPath("~/Uploads/Covers");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = "story-" + storyId + "-" + Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
            var physicalPath = Path.Combine(uploads, fileName);
            coverFile.SaveAs(physicalPath);

            var mapPath = Server.MapPath("~/App_Data/story-covers.txt");
            var lines = new List<string>();
            if (System.IO.File.Exists(mapPath))
            {
                lines = System.IO.File.ReadAllLines(mapPath).Where(line => !line.StartsWith(storyId + "|", StringComparison.Ordinal)).ToList();
            }
            lines.Add(storyId + "|/Uploads/Covers/" + fileName);
            System.IO.File.WriteAllLines(mapPath, lines);
        }

        private bool TrySaveChapterImage(int storyId, HttpPostedFileBase image, out string imageUrl, out string error)
        {
            imageUrl = null;
            error = null;

            if (image == null || image.ContentLength <= 0)
            {
                error = "Vui lòng chọn ảnh chương.";
                return false;
            }

            const int maxImageSize = 8 * 1024 * 1024;
            if (image.ContentLength > maxImageSize)
            {
                error = "Ảnh chương không được vượt quá 8 MB.";
                return false;
            }

            var extension = Path.GetExtension(image.FileName);
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(extension == null ? "" : extension.ToLowerInvariant()))
            {
                error = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.";
                return false;
            }

            var uploads = Server.MapPath("~/Uploads/Chapters");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = "story-" + storyId + "-chapter-" + Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
            image.SaveAs(Path.Combine(uploads, fileName));
            imageUrl = "/Uploads/Chapters/" + fileName;
            return true;
        }

        private bool IsChapterImageUrl(string imageUrl)
        {
            return !String.IsNullOrWhiteSpace(imageUrl)
                && imageUrl.StartsWith("/Uploads/Chapters/", StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteChapterImage(string imageUrl)
        {
            const string uploadPrefix = "/Uploads/Chapters/";
            if (!IsChapterImageUrl(imageUrl)) return;

            var fileName = Path.GetFileName(imageUrl);
            if (String.IsNullOrWhiteSpace(fileName)
                || !String.Equals(imageUrl, uploadPrefix + fileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var filePath = Path.Combine(Server.MapPath("~/Uploads/Chapters"), fileName);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        private string GetCoverUrl(int storyId)
        {
            var mapPath = Server.MapPath("~/App_Data/story-covers.txt");
            if (!System.IO.File.Exists(mapPath)) return null;

            var line = System.IO.File.ReadAllLines(mapPath).FirstOrDefault(l => l.StartsWith(storyId + "|", StringComparison.Ordinal));
            return line == null ? null : line.Substring(line.IndexOf('|') + 1);
        }

        private void RemoveCover(int storyId)
        {
            var url = GetCoverUrl(storyId);
            if (!String.IsNullOrEmpty(url))
            {
                var physicalPath = Server.MapPath(url);
                if (physicalPath != null && System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
            }

            var mapPath = Server.MapPath("~/App_Data/story-covers.txt");
            if (System.IO.File.Exists(mapPath))
            {
                var lines = System.IO.File.ReadAllLines(mapPath).Where(l => !l.StartsWith(storyId + "|", StringComparison.Ordinal));
                System.IO.File.WriteAllLines(mapPath, lines);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
