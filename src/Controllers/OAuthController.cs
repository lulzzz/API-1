#warning Reconstruct required!
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.API.Services;
using Aiursoft.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Aiursoft.API.Models.OAuthViewModels;
using Aiursoft.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Pylon.Services;
using Aiursoft.Pylon.Models;
using System.Linq;
using Microsoft.Extensions.Localization;
using Aiursoft.Pylon.Services.ToDeveloperServer;
using Aiursoft.Pylon.Models.API.OAuthAddressModels;
using Aiursoft.Pylon.Models.API.OAuthViewModels;
using Aiursoft.Pylon.Models.ForApps.AddressModels;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon.Exceptions;
using Aiursoft.Pylon.Models.Developer;

namespace Aiursoft.API.Controllers
{
    public class OAuthController : Controller
    {
        private readonly UserManager<APIUser> _userManager;
        private readonly SignInManager<APIUser> _signInManager;
        private readonly ILogger _logger;
        private readonly APIDbContext _dbContext;
        private readonly IStringLocalizer<OAuthController> _localizer;

        public OAuthController(
            UserManager<APIUser> userManager,
            SignInManager<APIUser> signInManager,
            ILoggerFactory loggerFactory,
            APIDbContext _context,
            IStringLocalizer<OAuthController> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<OAuthController>();
            _dbContext = _context;
            _localizer = localizer;
        }

        //http://localhost:53657/oauth/authorize?appid=29bf5250a6d93d47b6164ac2821d5009&redirect_uri=http%3A%2F%2Flocalhost%3A55771%2FAuth%2FAuthResult&response_type=code&scope=snsapi_base&state=http%3A%2F%2Flocalhost%3A55771%2FAuth%2FGoAuth#aiursoft_redirect
        [HttpGet]
        public async Task<IActionResult> Authorize(AuthorizeAddressModel model)
        {
            // Handle invalid model state.
            if(!ModelState.IsValid)
            {
                return View("AuthError");
            }
            var capp = (await ApiService.AppInfoAsync(model.appid)).App;
            if (capp == null)
            {
                // Handled by middleware
                return NotFound();
            }
            var url = new Uri(model.redirect_uri);
            var cuser = await GetCurrentUserAsync();
            // Wrong domain
            if (url.Host != capp.AppDomain && capp.DebugMode == false)
            {
                ModelState.AddModelError(string.Empty, "Redirect uri did not work in the valid domain!");
                _logger.LogInformation($"A request with appId {model.appid} is access wrong domian.");
                return View("AuthError");
            }
            // Signed in but have to input info.
            else if (cuser != null && capp.ForceInputPassword == false && model.forceConfirm != true)
            {
                return await FinishAuth(model.Convert(cuser.Email), capp.ForceConfirmation);
            }
            // Not signed in but we don't want his info
            else if (model.tryAutho == true)
            {
                return Redirect($"{url.Scheme}://{url.Host}:{url.Port}/?{Values.directShowString.Key}={Values.directShowString.Value}");
            }
            var viewModel = new AuthorizeViewModel(model.redirect_uri, model.state, model.appid, model.scope, model.response_type, capp.AppName, capp.AppIconAddress);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Authorize(AuthorizeViewModel model)
        {
            var capp = (await ApiService.AppInfoAsync(model.AppId)).App;
            if (capp == null)
            {
                // App id invalid
                return NotFound();
            }
            var mail = await _dbContext
                .UserEmails
                .Include(t => t.Owner)
                .SingleOrDefaultAsync(t => t.EmailAddress == model.Email.ToLower());
            //Email invalid
            if (mail == null)
            {
                ModelState.AddModelError(string.Empty, "Unknown user email.");
            }
            if (ModelState.IsValid)
            {
                var user = mail.Owner;
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    return await FinishAuth(model, capp.ForceConfirmation);
                }
                else if (result.RequiresTwoFactor)
                {
                    throw new NotImplementedException();
                }
                else if (result.IsLockedOut)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            model.Recover(capp.AppName, capp.AppIconAddress);
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> AuthorizeConfirm(AuthorizeConfirmAddressModel model)
        {
            var cuser = await GetCurrentUserAsync();
            if (ModelState.IsValid && cuser != null)
            {
                var capp = (await ApiService.AppInfoAsync(model.AppId)).App;
                var viewModel = new AuthorizeConfirmViewModel
                {
                    AppName = capp.AppName,
                    UserNickName = cuser.NickName,
                    AppId = model.AppId,
                    ToRedirect = model.ToRedirect,
                    State = model.State,
                    Scope = model.Scope,
                    ResponseType = model.ResponseType,
                    UserIcon = cuser.HeadImgUrl,
                    ViewOpenId = capp.ViewOpenId,
                    ViewPhoneNumber = capp.ViewPhoneNumber,
                    ChangePhoneNumber = capp.ChangePhoneNumber,
                    ConfirmEmail = capp.ConfirmEmail,
                    ChangeBasicInfo = capp.ChangeBasicInfo,
                    ChangePassword = capp.ChangePassword
                };
                return View(viewModel);
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AuthorizeConfirm(AuthorizeConfirmViewModel model)
        {
            var cuser = await GetCurrentUserAsync();
            if (ModelState.IsValid && cuser != null)
            {
                model.Email = cuser.Email;
                await cuser.GrantTargetApp(_dbContext, model.AppId);
                return await FinishAuth(model);
            }
            return View(model);
        }

        [HttpPost]
        [ForceValidateModelState]
        public async Task<IActionResult> PasswordAuth(PasswordAuthAddressModel model)
        {
            OAuthPack pack = null;
            var capp = (await ApiService.AppInfoAsync(model.AppId)).App;
            var mail = await _dbContext
                .UserEmails
                .Include(t => t.Owner)
                .SingleOrDefaultAsync(t => t.EmailAddress == model.Email);
            if (mail == null)
            {
                ModelState.AddModelError(string.Empty, "Unknown user email.");
            }
            if (ModelState.IsValid)
            {
                var user = mail.Owner;
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    if (await user.HasAuthorizedApp(_dbContext, model.AppId))
                    {
                        pack = await user.GeneratePack(_dbContext, model.AppId);
                    }
                    else
                    {
                        await user.GrantTargetApp(_dbContext, model.AppId);
                        pack = await user.GeneratePack(_dbContext, model.AppId);
                    }
                    return Json(new AiurValue<int>(pack.Code)
                    {
                        code = ErrorType.Success,
                        message = "Auth success."
                    });
                }
                else if (result.RequiresTwoFactor)
                {
                    throw new NotImplementedException();
                }
                else if (result.IsLockedOut)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // Issue: Can not response this error message.
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            return Json(new AiurProtocal
            {
                code = ErrorType.Unauthorized,
                message = "Incorrect username or password."
            });
        }

        public async Task<IActionResult> Register(AuthorizeAddressModel model)
        {
            if (ModelState.IsValid)
            {
                var capp = (await ApiService.AppInfoAsync(model.appid)).App;
                var viewModel = new RegisterViewModel()
                {
                    ToRedirect = model.redirect_uri,
                    State = model.state,
                    AppId = model.appid,
                    Scope = model.scope,
                    ResponseType = model.response_type,
                    AppImageUrl = capp.AppIconAddress
                };
                return View(viewModel);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new APIUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    NickName = model.Email.Split('@')[0],
                    PreferedLanguage = model.PreferedLanguage
                };
                bool exists = _dbContext.UserEmails.Exists(t => t.EmailAddress == model.Email.ToLower());
                if (exists)
                {
                    ModelState.AddModelError(string.Empty, $"A user with email: '{model.Email}' already exists!");
                    return View(model);
                }
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var primaryMail = new UserEmail
                    {
                        EmailAddress = model.Email.ToLower(),
                        OwnerId = user.Id
                    };
                    _dbContext.UserEmails.Add(primaryMail);
                    await _dbContext.SaveChangesAsync();
                    await _signInManager.SignInAsync(user, isPersistent: true);
                    return await FinishAuth(model);
                }
                AddErrors(result);
            }
            return View(model);
        }

        [HttpPost]
        [ForceValidateModelState]
        public async Task<IActionResult> AppRegister(AppRegisterAddressModel model)
        {
            var user = new APIUser { UserName = model.Email, Email = model.Email, NickName = model.Email.Split('@')[0], PreferedLanguage = "en" };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var primaryMail = new UserEmail
                {
                    EmailAddress = model.Email.ToLower(),
                    OwnerId = user.Id
                };
                _dbContext.UserEmails.Add(primaryMail);
                await _dbContext.SaveChangesAsync();
                return this.Protocal(ErrorType.Success, "Successfully created your account.");
            }
            return this.Protocal(ErrorType.NotEnoughResources, result.Errors.First().Description);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Signout()
        {
            await _signInManager.SignOutAsync();
            return Json(new AiurProtocal { message = "Successfully signed out!", code = ErrorType.Success });
        }

        public async Task<IActionResult> UserSignout(UserSignoutAddressModel model)
        {
            await _signInManager.SignOutAsync();
            return Redirect(model.ToRedirect);
        }

        public async Task<IActionResult> CodeToOpenId(CodeToOpenIdAddressModel model)
        {
            var AccessToken = await _dbContext.AccessToken.SingleOrDefaultAsync(t => t.Value == model.AccessToken);
            if (AccessToken == null)
            {
                return Json(new AiurProtocal { message = "Not a valid access token!", code = ErrorType.Unauthorized });
            }

            var targetPack = await _dbContext
                .OAuthPack
                .Where(t => t.IsUsed == false)
                .SingleOrDefaultAsync(t => t.Code == model.Code);

            if (targetPack == null)
            {
                return Json(new AiurProtocal { message = "Invalid Code.", code = ErrorType.WrongKey });
            }
            if (targetPack.ApplyAppId != AccessToken.ApplyAppId)
            {
                return Json(new AiurProtocal { message = "The app granted code is not the app granting access token!", code = ErrorType.Unauthorized });
            }
            var capp = (await ApiService.AppInfoAsync(targetPack.ApplyAppId)).App;
            if (!capp.ViewOpenId)
            {
                return this.Protocal(ErrorType.Unauthorized, "The app doesn't have view open id permission.");
            }
            targetPack.IsUsed = true;
            await _dbContext.SaveChangesAsync();
            var viewModel = new CodeToOpenIdViewModel
            {
                openid = targetPack.UserId,
                scope = "scope",
                message = "Successfully get user openid",
                code = ErrorType.Success
            };
            return Json(viewModel);
        }

        public async Task<IActionResult> UserInfo(UserInfoAddressModel model)
        {
            var target = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.access_token);

            if (target == null)
            {
                return Json(new AiurProtocal { message = "Invalid Access Token!", code = ErrorType.WrongKey });
            }
            else if (!target.IsAlive)
            {
                return Json(new AiurProtocal { message = "Access Token is timeout!", code = ErrorType.Timeout });
            }
            var cuser = await _userManager.FindByIdAsync(model.openid);
            var viewModel = new UserInfoViewModel
            {
                code = 0,
                message = "Successfully get target user info.",
                User = cuser
            };
            return Json(viewModel);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task<APIUser> GetCurrentUserAsync(string Email)
        {
            var mail = await _dbContext
                .UserEmails
                .Include(t => t.Owner)
                .SingleOrDefaultAsync(t => t.EmailAddress == Email);
            return mail.Owner;
        }

        private async Task<APIUser> GetCurrentUserAsync()
        {
            return await _dbContext.Users
                .SingleOrDefaultAsync(t => t.UserName == User.Identity.Name);
        }

        private async Task<IActionResult> FinishAuth(IOAuthInfo model, bool forceGrant = false)
        {
            var cuser = await GetCurrentUserAsync(model.Email);
            await _userManager.UpdateAsync(cuser);
            if (await cuser.HasAuthorizedApp(_dbContext, model.AppId) && forceGrant == false)
            {
                var pack = await cuser.GeneratePack(_dbContext, model.AppId);
                var url = new AiurUrl(model.GetRegexRedirectUrl(), new AuthResultAddressModel
                {
                    code = pack.Code,
                    state = model.State
                });
                return Redirect(url);
            }
            else
            {
                return RedirectToAction(nameof(AuthorizeConfirm), new AuthorizeConfirmAddressModel
                {
                    AppId = model.AppId,
                    State = model.State,
                    ToRedirect = model.ToRedirect,
                    Scope = model.Scope,
                    ResponseType = model.ResponseType
                });
            }
        }

        private RedirectResult Redirect(AiurUrl url)
        {
            return Redirect(url.ToString());
        }
    }
}
