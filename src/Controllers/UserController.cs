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
using Microsoft.Extensions.Configuration;

namespace Aiursoft.API.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<APIUser> _userManager;
        private readonly SignInManager<APIUser> _signInManager;
        private readonly ILogger _logger;
        public readonly APIDbContext _dbContext;
        private readonly IStringLocalizer<ApiController> _localizer;
        private readonly AiurEmailSender _emailSender;
        private readonly AiurSMSSender _smsSender;
        private readonly IConfiguration _configuration;


        public UserController(
            UserManager<APIUser> userManager,
            SignInManager<APIUser> signInManager,
            ILoggerFactory loggerFactory,
            APIDbContext _context,
            IStringLocalizer<ApiController> localizer,
            AiurEmailSender emailSender,
            AiurSMSSender smsSender,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<ApiController>();
            _dbContext = _context;
            _localizer = localizer;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _configuration = configuration;
        }

        [ForceValidateModelState]
        [ForceValidateAccessToken]
        [AiurExceptionHandler]
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
            if (!string.IsNullOrEmpty(model.NewBio))
            {
                targetUser.Bio = model.NewBio;
            }
            await _dbContext.SaveChangesAsync();
            return Json(new AiurProtocal { code = ErrorType.Success, message = "Successfully changed this user's nickname!" });
        }

        [ForceValidateModelState]
        [ForceValidateAccessToken]
        [AiurExceptionHandler]
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

        [ForceValidateModelState]
        [ForceValidateAccessToken]
        [AiurExceptionHandler]
        public async Task<IActionResult> ViewPhoneNumber(ViewPhoneNumberAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await ApiService.AppInfoAsync(accessToken.ApplyAppId);
            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            if (targetUser == null)
            {
                return this.Protocal(ErrorType.NotFound, "Could not find target user.");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { code = ErrorType.Unauthorized, message = "This user did not grant your app!" });
            }
            if (!app.App.ViewPhoneNumber)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to view users' phone number.");
            }
            return Json(new AiurValue<string>(targetUser.PhoneNumber)
            {
                code = ErrorType.Success,
                message = "Successfully get the target user's phone number."
            });
        }

        [ForceValidateModelState]
        [ForceValidateAccessToken]
        [AiurExceptionHandler]
        public async Task<JsonResult> SetPhoneNumber(SetPhoneNumberAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await ApiService.AppInfoAsync(accessToken.ApplyAppId);
            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            if (targetUser == null)
            {
                return this.Protocal(ErrorType.NotFound, "Could not find target user.");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { code = ErrorType.Unauthorized, message = "This user did not grant your app!" });
            }
            if (!app.App.ChangePhoneNumber)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to set users' phone number.");
            }
            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                targetUser.PhoneNumber = string.Empty;
            }
            else
            {
                targetUser.PhoneNumber = model.Phone;
            }
            await _userManager.UpdateAsync(targetUser);
            return this.Protocal(ErrorType.Success, "Successfully set the user's PhoneNumber!");
        }

        [HttpGet]
        public IActionResult SelectPasswordMethod()
        {
            return View();
        }

        #region Forgot Password with email
        [HttpGet]
        public IActionResult ForgotPasswordViaEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordViaEmail(ForgotPasswordViaEmailViewModel model)
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
                    $"Please reset your password by clicking <a href='{callbackUrl}'>here</a>", _configuration["emailpassword"]);
                return RedirectToAction(nameof(ForgotPasswordSent));
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult ForgotPasswordSent()
        {
            return View();
        }
        #endregion
        #region Forgot Password with SMS
        [HttpGet]
        public IActionResult ForgotPasswordViaSMS()
        {
            var model = new ForgotPasswordViaEmailViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordViaSMS(ForgotPasswordViaEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    model.ModelStateValid = false;
                    ModelState.AddModelError("", $"We can't find an account with email:`{model.Email}`!");
                    return View(model);
                }
                if (user.PhoneNumberConfirmed == false)
                {
                    model.ModelStateValid = false;
                    ModelState.AddModelError("", "Your account did not bind a valid phone number!");
                    return View(model);
                }
                var code = StringOperation.RandomString(6);
                user.SMSPasswordResetToken = code;
                await _userManager.UpdateAsync(user);
                await _smsSender.SendAsync(user.PhoneNumber, code + " is your Aiursoft password reset code.");
                return RedirectToAction(nameof(EnterSMSCode), new { Email = model.Email });
            }
            return View(model);
        }

        public async Task<IActionResult> EnterSMSCode(string Email)
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null || user.PhoneNumberConfirmed == false)
            {
                return NotFound();
            }
            var phoneLast = user.PhoneNumber.Substring(user.PhoneNumber.Length - 4);
            var model = new EnterSMSCodeViewModel
            {
                Email = Email,
                PhoneLast = phoneLast
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EnterSMSCode(EnterSMSCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ModelStateValid = false;
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user.SMSPasswordResetToken.ToLower().Trim() == model.Code.ToLower().Trim())
            {
                user.SMSPasswordResetToken = string.Empty;
                await _userManager.UpdateAsync(user);
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                return RedirectToAction(nameof(ResetPassword), new { code = token });
            }
            else
            {
                model.ModelStateValid = false;
                ModelState.AddModelError("", "Your code is not correct and we can't help you reset your password!");
                return View(model);
            }
        }
        #endregion
        #region Reset password
        [HttpGet]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return RedirectToAction(nameof(SelectPasswordMethod));
            }
            var model = new ResetPasswordViewModel
            {
                Code = code
            };
            return View();
        }

        [HttpPost]
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
        #endregion

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}