using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using comic_website.Models;

namespace comic_website.Controllers
{
public class UserController : Controller
{
    private readonly WebDocTruyenEntities db =
        new WebDocTruyenEntities();

    public ActionResult Login()
    {
        return View();
    }

    public ActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Register(string username, string email, string password, string confirmPassword)
    {
        username = username == null ? String.Empty : username.Trim();
        email = email == null ? String.Empty : email.Trim();

        if (String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(email) ||
            String.IsNullOrWhiteSpace(password) || String.IsNullOrWhiteSpace(confirmPassword))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
            return View();
        }

        if (password.Length < 6)
        {
            ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự";
            return View();
        }

        if (!password.Equals(confirmPassword))
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp";
            return View();
        }

        if (db.Users.Any(u => u.Email == email))
        {
            ViewBag.Error = "Email đã được sử dụng";
            return View();
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = HashPassword(password)
        };

        db.Users.Add(user);
        db.SaveChanges();

        TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Login(string email, string password)
    {
        if (String.IsNullOrWhiteSpace(email) || String.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập email và mật khẩu";
            return View();
        }

        email = email.Trim();
        var user = db.Users.FirstOrDefault(u => u.Email == email);

        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            user = null;
        }

        // Falls back to legacy passwords only for existing accounts; new registrations use PBKDF2.
        if (user == null)
        {
            var legacyUser = db.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == password);
            if (legacyUser != null)
            {
                user = legacyUser;
            }
        }


        if (user != null)
        {
            Session["UserId"] = user.UserId;
            Session["Username"] = user.Username;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Logout()
    {
        Session.Remove("UserId");
        Session.Remove("Username");
        return RedirectToAction("Login");
    }

    private static string HashPassword(string password)
    {
        const int iterations = 100000;
        var salt = new byte[16];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt);
        }

        byte[] hash;
        using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
        {
            hash = deriveBytes.GetBytes(32);
        }

        return "PBKDF2|" + iterations + "|" + Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        if (String.IsNullOrEmpty(storedHash) || !storedHash.StartsWith("PBKDF2|"))
        {
            return false;
        }

        var parts = storedHash.Split('|');
        if (parts.Length != 4)
        {
            return false;
        }

        int iterations;
        byte[] salt;
        byte[] expectedHash;

        try
        {
            iterations = Int32.Parse(parts[1]);
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualHash;
        using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
        {
            actualHash = deriveBytes.GetBytes(expectedHash.Length);
        }

        if (actualHash.Length != expectedHash.Length)
        {
            return false;
        }

        var mismatch = 0;
        for (var i = 0; i < actualHash.Length; i++)
        {
            mismatch |= actualHash[i] ^ expectedHash[i];
        }

        return mismatch == 0;
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
