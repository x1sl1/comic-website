using Comic_web.Models;
using System.Web.Mvc;
using System.Linq;

public class UserController : Controller
{
    private WebDocTruyenEntities db =
        new WebDocTruyenEntities();

    public ActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Login(string email, string password)
    {
        var user = db.Users
            .FirstOrDefault(u =>
                u.Email == email &&
                u.PasswordHash == password);

        if (user != null)
        {
            Session["UserId"] = user.UserId;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
        return View();
    }
    public ActionResult Logout()
    {
        Session.Clear();

        return RedirectToAction("Login");
    }
}
