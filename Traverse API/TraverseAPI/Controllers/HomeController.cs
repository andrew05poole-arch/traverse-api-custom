#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Web.Mvc;
using System.Web.Security;
using TraverseApi.Properties;
#endregion Using Directives

namespace TraverseApi
{
    public class HomeController : Controller
    {
        #region Action Results
        public ActionResult Index()
        {
            ApiUser user = User as ApiUser;
            if (user == null)
                return RedirectToAction("Login", "Home");

            if (user.ResetPassword.GetValueOrDefault())
                return RedirectToAction("ChangePwd", "Home");

            if (user.UserStatus == ApiUserStatus.Renew)
                return RedirectToAction("Register");

            ViewBag.Title = TravApiConfig.UserPortalTitle;
            ViewBag.UserStatus = user.UserStatus.ToString();
            return View(User);
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            ViewBag.Title = TravApiConfig.UserPortalTitle;
            ViewBag.UserStatus = "";
            return View();
        }

        public ActionResult Document()
        {
            if ((User as ApiUser) == null)
                return RedirectToAction("Login", "Home");

            ViewBag.Title = TravApiConfig.UserPortalTitle;
            ViewBag.UserStatus = (User as ApiUser).UserStatus.ToString();
            return Redirect(Url.Content("~/swagger/ui/index"));
        }

        public virtual ActionResult Register()
        {
            if ((User as ApiUser) == null)
                return RedirectToAction("Login", "Home");

            ViewBag.Title = TravApiConfig.UserPortalTitle;
            ViewBag.UserStatus = (User as ApiUser).UserStatus.ToString();
            return View(new ApiLogin());
        }

        public virtual ActionResult ChangePwd()
        {
            if ((User as ApiUser) == null)
                return RedirectToAction("Login", "Home");

            ViewBag.Title = TravApiConfig.UserPortalTitle;
            ViewBag.UserStatus = (User as ApiUser).UserStatus.ToString();
            return View(new ApiChangePassword());
        }

        public ActionResult Logout()
        {
            if ((User as ApiUser) == null)
                return RedirectToAction("Login", "Home");

            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(ApiLogin login, string ReturnUrl = "")
        {
            string id = null;
            //Validate user information for login
            if (ApiLogin.ValidateUserCred(login, ref id))
            {
                //Build cookie response
                FormsAuthentication.SetAuthCookie(id, login.RememberMe);

                //Send user either to their desired location or default Home page
                if (Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
                ModelState.AddModelError("", Resources.ApiInvalidCredential);

            ModelState.Remove("Password");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Register(ApiLogin login)
        {
            ApiUser user = null;
            if ((user = User as ApiUser) == null)
                return RedirectToAction("Login", "Home");

            user.GenerateUserToken(true);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual ActionResult ChangePwd(ApiChangePassword login)
        {
            ApiUser user = null;
            if ((user = User as ApiUser) == null)
                return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid)
                return View();

            if (login.OldPassword != user.UnencryptedPassword)
            {
                ModelState.AddModelError("", "Invalid current password provided");
                return View();
            }

            if (login.Password == user.UnencryptedPassword)
            {
                ModelState.AddModelError("", "New password cannot be the same as the old password");
                return View();
            }

            user.ChangePassword(login.Password);

            if (user.ClientId.GetValueOrDefault() == Guid.Empty ||
                user.UserStatus == ApiUserStatus.Renew ||
                user.UserStatus == ApiUserStatus.New)
                return RedirectToAction("Register");

            return RedirectToAction("Index");
        }
        #endregion Action Results
    }
}
