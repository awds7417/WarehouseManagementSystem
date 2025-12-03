using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepo = new UserRepository();

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string userName, string password, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "請輸入帳號與密碼。");
                return View();
            }

            User user = _userRepo.GetByUserName(userName);
            if (user == null)
            {
                ModelState.AddModelError("", "帳號或密碼錯誤。");
                return View();
            }

            // 雜湊輸入密碼再比對
            //string inputHash = PasswordHelper.HashPassword(password);
            //if (!string.Equals(user.PasswordHash, inputHash, StringComparison.OrdinalIgnoreCase))
            //{
            //    ModelState.AddModelError("", "帳號或密碼錯誤。");
            //    return View();
            //}

            // 暫時用明文比對
            if (user.PasswordHash != password)
            {
                ModelState.AddModelError("", "帳號或密碼錯誤。");
                return View();
            }

            // 建立 FormsAuthentication Cookie
            FormsAuthentication.SetAuthCookie(user.UserName, false);

            // 可把 IsAdmin 等資訊塞在 Session 裡
            Session["DisplayName"] = string.IsNullOrEmpty(user.DisplayName) ? user.UserName : user.DisplayName;
            Session["IsAdmin"] = user.IsAdmin;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        // (可選) GET: /Account/Register 只允許 Admin 使用
        [Authorize]
        public ActionResult Register()
        {
            // 只給管理者使用
            object isAdminObj = Session["IsAdmin"];
            bool isAdmin = (isAdminObj is bool) && (bool)isAdminObj;

            if (!isAdmin)
            {
                // 權限不足的處理
                return new HttpUnauthorizedResult();
            }
            return View();
        }

        // (可選) POST: /Account/Register
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string userName, string password, string displayName, bool isAdmin = false)
        {
            object isAdminObj = Session["IsAdmin"];

            if (isAdminObj is bool)
            {
                isAdmin = (bool)isAdminObj;
            }

            if (!isAdmin)
            {
                return new HttpUnauthorizedResult();
            }


            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "請輸入帳號與密碼。");
                return View();
            }

            User existing = _userRepo.GetByUserName(userName);
            if (existing != null)
            {
                ModelState.AddModelError("", "此帳號已存在。");
                return View();
            }

            User newUser = new User();
            newUser.UserName = userName;
            newUser.PasswordHash = PasswordHelper.HashPassword(password);
            newUser.DisplayName = displayName;
            newUser.IsAdmin = isAdmin;

            _userRepo.Insert(newUser);

            ViewBag.Message = "帳號建立成功。";
            return View();
        }
    }
}
