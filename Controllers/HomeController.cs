using System;
using System.Collections.Generic;
using System.IO;
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

            ViewBag.CoverUrls = GetStoryCoverUrls(stories.Select(story => story.StoryId));
            return View(stories);
        }

        private IDictionary<int, string> GetStoryCoverUrls(IEnumerable<int> storyIds)
        {
            var coverUrls = new Dictionary<int, string>();
            var storyIdSet = new HashSet<int>(storyIds);
            var mapPath = Server.MapPath("~/App_Data/story-covers.txt");
            if (!System.IO.File.Exists(mapPath)) return coverUrls;

            foreach (var line in System.IO.File.ReadLines(mapPath))
            {
                if (String.IsNullOrWhiteSpace(line)) continue;

                var separator = line.IndexOf('|');
                int storyId;
                if (separator <= 0
                    || !int.TryParse(line.Substring(0, separator), out storyId)
                    || !storyIdSet.Contains(storyId))
                {
                    continue;
                }

                var coverUrl = line.Substring(separator + 1).Trim();
                if (!String.IsNullOrWhiteSpace(coverUrl))
                {
                    coverUrls[storyId] = coverUrl;
                }
            }

            return coverUrls;
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