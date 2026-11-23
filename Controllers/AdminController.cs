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
        private static readonly string[] StoryCategoryNames =
        {
            "Hành động", "Phiêu lưu", "Romance", "Hài hước", "Fantasy",
            "Manhwa", "Siêu nhiên", "Kinh dị", "Trinh thám", "Truyện chữ"
        };

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

            ViewBag.Categories = BuildStoryCategoryOptions(null);
            return View(new Story());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStory(Story story, string categoryName, HttpPostedFileBase coverFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            if (String.IsNullOrWhiteSpace(story.Title))
            {
                ModelState.AddModelError("Title", "Tên truyện là bắt buộc.");
            }

            categoryName = GetCanonicalStoryCategory(categoryName);
            if (categoryName == null)
            {
                ModelState.AddModelError("categoryName", "Vui lòng chọn thể loại hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                var category = GetOrCreateStoryCategory(categoryName);
                story.CategoryId = category.CategoryId;
                story.CreatedAt = DateTime.Now;
                db.Stories.Add(story);
                db.SaveChanges();

                SaveCover(story.StoryId, coverFile);
                TempData["Success"] = "Đã thêm truyện mới.";
                return RedirectToAction("Stories");
            }

            ViewBag.Categories = BuildStoryCategoryOptions(categoryName);
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

            var currentCategory = db.Categories.Find(story.CategoryId);
            ViewBag.Categories = BuildStoryCategoryOptions(currentCategory == null ? null : currentCategory.CategoryName);
            ViewBag.CoverUrl = GetCoverUrl(id.Value);
            return View(story);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStory(Story story, string categoryName, HttpPostedFileBase coverFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var existing = db.Stories.Find(story.StoryId);
            if (existing == null) return HttpNotFound();

            existing.Title = story.Title;
            existing.Author = story.Author;
            existing.Description = story.Description;

            if (String.IsNullOrWhiteSpace(existing.Title))
            {
                ModelState.AddModelError("Title", "Tên truyện là bắt buộc.");
            }

            categoryName = GetCanonicalStoryCategory(categoryName);
            if (categoryName == null)
            {
                ModelState.AddModelError("categoryName", "Vui lòng chọn thể loại hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                var category = GetOrCreateStoryCategory(categoryName);
                existing.CategoryId = category.CategoryId;
                db.SaveChanges();
                SaveCover(existing.StoryId, coverFile);
                TempData["Success"] = "Đã cập nhật thông tin truyện.";
                return RedirectToAction("Stories");
            }

            ViewBag.Categories = BuildStoryCategoryOptions(categoryName);
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
        public ActionResult CreateChapter(int storyId, int chapterNumber, string title, HttpPostedFileBase[] chapterImages)
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

            List<string> imageUrls;
            string uploadError;
            if (!TrySaveChapterImages(storyId, chapterNumber, chapterImages, out imageUrls, out uploadError))
            {
                TempData["Error"] = uploadError;
                return RedirectToAction("Chapters", new { storyId = storyId });
            }

            db.Chapters.Add(new Chapter
            {
                StoryId = storyId,
                ChapterNumber = chapterNumber,
                Title = title,
                Content = ChapterImageContent.Serialize(imageUrls)
            });
            db.SaveChanges();

            TempData["Success"] = "Đã tải lên chương mới.";
            return RedirectToAction("Chapters", new { storyId = storyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditChapter(int chapterId, int chapterNumber, string title, HttpPostedFileBase[] chapterImages)
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

            var oldContent = chapter.Content;
            var newContent = oldContent;
            var hasNewImages = chapterImages != null
                && chapterImages.Any(image => image != null && image.ContentLength > 0);
            if (hasNewImages)
            {
                List<string> imageUrls;
                string uploadError;
                if (!TrySaveChapterImages(chapter.StoryId, chapterNumber, chapterImages, out imageUrls, out uploadError))
                {
                    TempData["Error"] = uploadError;
                    return RedirectToAction("Chapters", new { storyId = chapter.StoryId });
                }

                newContent = ChapterImageContent.Serialize(imageUrls);
            }

            chapter.ChapterNumber = chapterNumber;
            chapter.Title = title;
            chapter.Content = newContent;
            db.SaveChanges();
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.Chapters SET UpdatedAt = SYSUTCDATETIME() WHERE ChapterId = @p0",
                chapterId);

            if (!String.Equals(oldContent, newContent, StringComparison.Ordinal))
            {
                DeleteChapterImages(oldContent);
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
            var imageContent = chapter.Content;
            db.Chapters.Remove(chapter);
            db.SaveChanges();
            DeleteChapterImages(imageContent);

            TempData["Success"] = "Đã xóa chương.";
            return RedirectToAction("Chapters", new { storyId = storyId });
        }

        private bool IsAdmin()
        {
            return Session["IsAdmin"] is bool && (bool)Session["IsAdmin"];
        }

        private static SelectList BuildStoryCategoryOptions(string selectedCategoryName)
        {
            var categories = StoryCategoryNames.ToList();
            if (!String.IsNullOrWhiteSpace(selectedCategoryName)
                && !categories.Any(name => String.Equals(name, selectedCategoryName, StringComparison.OrdinalIgnoreCase)))
            {
                categories.Insert(0, selectedCategoryName);
            }

            return new SelectList(categories, selectedCategoryName);
        }

        private static string GetCanonicalStoryCategory(string categoryName)
        {
            if (String.IsNullOrWhiteSpace(categoryName)) return null;

            return StoryCategoryNames.FirstOrDefault(name =>
                String.Equals(name, categoryName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private Category GetOrCreateStoryCategory(string categoryName)
        {
            var category = db.Categories.FirstOrDefault(item => item.CategoryName == categoryName);
            if (category != null) return category;

            category = new Category { CategoryName = categoryName };
            db.Categories.Add(category);
            db.SaveChanges();
            return category;
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

        private bool TrySaveChapterImages(int storyId, int chapterNumber, HttpPostedFileBase[] files, out List<string> imageUrls, out string error)
        {
            imageUrls = new List<string>();
            error = null;

            var images = (files ?? new HttpPostedFileBase[0])
                .Where(image => image != null && image.ContentLength > 0)
                .OrderBy(image => Path.GetFileName(image.FileName), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (images.Count == 0)
            {
                error = "Vui lòng chọn ít nhất một ảnh dải tranh.";
                return false;
            }

            const int maxImageSize = 8 * 1024 * 1024;
            const long maxTotalSize = 95L * 1024 * 1024;
            if (images.Count > 60)
            {
                error = "Mỗi chương được tải lên tối đa 60 ảnh.";
                return false;
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            long totalSize = 0;
            foreach (var image in images)
            {
                if (image.ContentLength > maxImageSize)
                {
                    error = "Mỗi ảnh dải tranh không được vượt quá 8 MB.";
                    return false;
                }

                totalSize += image.ContentLength;
                var extension = Path.GetExtension(image.FileName);
                if (!allowed.Contains(extension == null ? "" : extension.ToLowerInvariant()))
                {
                    error = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.";
                    return false;
                }
            }

            if (totalSize > maxTotalSize)
            {
                error = "Tổng dung lượng ảnh của một lần tải không được vượt quá 95 MB.";
                return false;
            }

            var uploads = Server.MapPath("~/Uploads/Chapters");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            for (var index = 0; index < images.Count; index++)
            {
                var image = images[index];
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                var fileName = "story-" + storyId
                    + "-chapter-" + chapterNumber
                    + "-page-" + (index + 1).ToString("D3")
                    + "-" + Guid.NewGuid().ToString("N") + extension;
                image.SaveAs(Path.Combine(uploads, fileName));
                imageUrls.Add("/Uploads/Chapters/" + fileName);
            }

            return true;
        }

        private void DeleteChapterImages(string content)
        {
            foreach (var imageUrl in ChapterImageContent.GetImageUrls(content))
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(Server.MapPath("~/Uploads/Chapters"), fileName);

                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }
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
