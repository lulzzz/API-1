using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Aiursoft.Pylon.Models;
using Microsoft.AspNetCore.Identity;
using Aiursoft.API.Models;
using Aiursoft.API.Services;
using Microsoft.Extensions.Logging;
using Aiursoft.API.Data;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Pylon.Services;
using Aiursoft.Pylon.Services.ToDeveloperServer;
using Aiursoft.Pylon.Models.API.ApiViewModels;
using Aiursoft.Pylon.Models.API.ApiAddressModels;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Models.API;
using Aiursoft.Pylon.Models.API.UserAddressModels;
using Aiursoft.API.Attributes;
using Aiursoft.API.Models.UserViewModels;

namespace Aiursoft.API.Controllers
{
    [AiurRequireHttps]
    [AiurExceptionHandler]
    public class UserController : AiurController
    {
        private readonly UserManager<APIUser> _userManager;
        private readonly SignInManager<APIUser> _signInManager;
        private readonly ILogger _logger;
        public readonly APIDbContext _dbContext;
        private readonly IStringLocalizer<ApiController> _localizer;
        private readonly AiurEmailSender _emailSender;


        public UserController(
            UserManager<APIUser> userManager,
            SignInManager<APIUser> signInManager,
            ILoggerFactory loggerFactory,
            APIDbContext _context,
            IStringLocalizer<ApiController> localizer,
            AiurEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<ApiController>();
            _dbContext = _context;
            _localizer = localizer;
            _emailSender = emailSender;
        }

        [ForceValidateModelState]
        [ForceValidateAccessToken]
        public async Task<JsonResult> ChangeProfile(ChangeProfileAddressModel model)
        {
            var target = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == target.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { code = ErrorType.Unauthorized, message = "This user did not grant your app!" });
            }

            if (!string.IsNullOrEmpty(model.NewNickName))
            {
                targetUser.NickName = model.NewNickName;
            }
            if (!string.IsNullOrEmpty(model.NewIconAddress))
            {
                targetUser.HeadImgUrl = model.NewIconAddress;
            }

            await _dbContext.SaveChangesAsync();
            return Json(new AiurProtocal { code = ErrorType.Success, message = "Successfully changed this user's nickname!" });
        }

        [ForceValidateModelState]
        [ForceValidateAccessToken]
        public async Task<JsonResult> ChangePassword(ChangePasswordAddressModel model)
        {
            var target = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == target.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { code = ErrorType.Unauthorized, message = "This user did not grant your app!" });
            }

            var result = await _userManager.ChangePasswordAsync(targetUser, model.OldPassword, model.NewPassword);
            await _userManager.UpdateAsync(targetUser);
            if (result.Succeeded)
            {
                return Json(new AiurProtocal { code = ErrorType.Success, message = "Successfully changed this user's password!" });
            }
            else
            {
                return Json(new AiurProtocal { code = ErrorType.WrongKey, message = result.Errors.First().Description });
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return RedirectToAction(nameof(ForgotPasswordSent));
                }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = new AiurUrl(Values.ApiServerAddress, "User", nameof(ResetPassword), new
                {
                    code = code,
                    userId = user.Id
                });
                await _emailSender.SendEmail(model.Email, "Reset Password", 
                    $"Please reset your password by clicking <a href='{callbackUrl}'>here</a>");
                return RedirectToAction(nameof(ForgotPasswordSent));
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordSent()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }
            var model = new ResetPasswordViewModel();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}