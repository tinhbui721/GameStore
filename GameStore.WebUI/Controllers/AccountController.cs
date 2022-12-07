using GameStore.Domain;
using GameStore.Domain.Identity;
using GameStore.Domain.Infrastructure;
using GameStore.Domain.Model;
using GameStore.WebUI.Areas.Admin.Models.DTO;
using GameStore.WebUI.Helper;
using GameStore.WebUI.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.AspNet.Identity.EntityFramework;

namespace GameStore.WebUI.Controllers
{

    public class AccountController : BaseController
    {
        public static string codeTemp;
        public static string passWord;

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = UserManager.FindByEmail(model.Email);
                if (user != null)
                {
                    ModelState.AddModelError("", "User with this email address has already existed! Please try another email address!");
                    return View(model);
                }
                user = UserManager.FindByName(model.UserName);
                if (user != null)
                {
                    ModelState.AddModelError("", "The User Name you specified is already existing! Please try with another user name!");
                    return View(model);
                }
                if (model != null)
                {
                    user = new AppUser { Email = model.Email, UserName = model.UserName, Membership = model.Membership, EmailConfirmed = false };
                    var result = UserManager.CreateAsync(user, model.Password);
                    passWord = model.Password;
                    if (result.Result.Succeeded)
                    {
                        var newUser = UserManager.FindByEmail(model.Email);
                        var provider = new DpapiDataProtectionProvider("GameStore.WebUI");
                        UserManager.UserTokenProvider = new DataProtectorTokenProvider<AppUser>(provider.Create("Confirmation"));
                        string code = UserManager.GenerateEmailConfirmationToken(newUser.Id);
                        codeTemp = code;
                        var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = newUser.Id, code = code }, protocol: Request.Url.Scheme);

                        SendMail.SendEMail(user.Email, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
                        return View("NotificationConfirmEmail");
                    }
                    else
                    {
                        AddErrors(result.Result);
                    }
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(LoginViewModel model)
        {
            var user = UserManager.FindByEmail(model.Email);
            if (user != null)
            {
                Random random = new Random();
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                string newPass = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
                user.PasswordHash = UserManager.HashPassword(newPass);
                UserManager.Update(user);
                SendMail.SendEMail(user.Email, "GameStore reset password", "Your new password is: " + newPass);
                ViewBag.code = 1;
            }
            else
            {
                ViewBag.code = 0;
            }

            return View("ForgotPassword");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ResendConfirm(RegisterViewModel model)
        {
            var newUser = UserManager.FindByEmail(model.Email);
            var provider = new DpapiDataProtectionProvider("GameStore.WebUI");
            UserManager.UserTokenProvider = new DataProtectorTokenProvider<AppUser>(provider.Create("Confirmation"));
            string code = UserManager.GenerateEmailConfirmationToken(newUser.Id);
            codeTemp = code;
            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = newUser.Id, code = code }, protocol: Request.Url.Scheme);
            SendMail.SendEMail(newUser.Email, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
            return View("NotificationConfirmEmail");
        }

        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId != null && code == codeTemp)
            {
                AppUser appUser = UserManager.FindById(userId);
                appUser.EmailConfirmed = true;
                UserManager.Update(appUser);
                LoginViewModel loginViewModel = new LoginViewModel();
                loginViewModel.Email = appUser.Email;
                loginViewModel.Password = passWord;
                return await Login(loginViewModel, null);
                //ViewBag.message = "Xác thực email thành công";
                //return View("ConfirmEmail");
            }
            else
            {
                ViewBag.message = "Xác thực email không thành công";
                return View("ConfirmEmail");
            }
        }

        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessCreditResponse(String TransId, String TransAmount, String StatusCode, String AppHash)
        {
            String AppId = ConfigurationHelper.GetAppId();
            String SharedKey = ConfigurationHelper.GetSharedKey();

            if (CreditAuthorizationClient.VerifyServerResponseHash(AppHash, SharedKey, AppId, TransId, TransAmount, StatusCode))
            {
                switch (StatusCode)
                {
                    case ("A"): ViewBag.TransactionStatus = "Transaction Approved!"; break;
                    case ("D"): ViewBag.TransactionStatus = "Transaction Denied!"; break;
                    case ("C"): ViewBag.TransactionStatus = "Transaction Cancelled!"; break;
                }
            }
            else
            {
                ViewBag.TransactionStatus = "Hash Verification failed... something went wrong.";
            }


            if (StatusCode.Equals("A"))
            {
                RegisterViewModel model = (RegisterViewModel)Session["Register"];
                if (model != null)
                {
                    var user = new AppUser { Email = model.Email, UserName = model.UserName, Membership = model.Membership };
                    var result = await UserManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        var newUser = UserManager.FindByEmail(model.Email);
                        var identity = await UserManager.CreateIdentityAsync(newUser, DefaultAuthenticationTypes.ApplicationCookie);
                        AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = false }, identity);

                        System.Web.HttpContext.Current.Cache.Remove("UserList");
                        Session["Register"] = null;
                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link
                        //string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                        //await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");


                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View();
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    {
                        var user = UserManager.FindByEmail(model.Email);
                        var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                        AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = false }, identity);
                        GetOrderCount(user.Id);
                        if (!user.EmailConfirmed)
                        {
                            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                            return View("NotificationConfirmEmail");
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(returnUrl))
                                return RedirectToLocal(returnUrl);
                            else
                                return RedirectToAction("Index", "Home");
                        }
                    }
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Log in failed, please check you email and password!");
                    return View(model);
            }
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session["OrderCount"] = 0;
            Session["CartCount"] = 0;
            return RedirectToAction("Index", "Home");
        }

        private void GetOrderCount(string id)
        {
            int count = 0;
            using (GameStoreDBContext context = new GameStoreDBContext())
            {
                count = context.Orders.Where(o => o.UserId == id).Count();
            }
            Session["OrderCount"] = count;
            Session["CartCount"] = 0;
        }

        [Authorize]
        public ActionResult MemberProfile()
        {
            UserDTO user = new UserDTO();
            using (GameStoreDBContext context = new GameStoreDBContext())
            {
                AppUser u = context.Users.Find(User.Identity.GetUserId());
                user = new UserDTO { Id = u.Id, Email = u.Email, UserName = u.UserName, Membership = u.Membership };
            }
            return View(user);
        }

        //
        // GET: /Manage/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", "Home");
            }
            AddErrors(result);
            return View(model);
        }

    }
}