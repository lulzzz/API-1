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
using API.Models;
using API.Services;
using Microsoft.Extensions.Logging;
using API.Data;
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
using API.Attributes;

namespace API.Controllers
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

        public UserController(
            UserManager<APIUser> userManager,
            SignInManager<APIUser> signInManager,
            ILoggerFactory loggerFactory,
            APIDbContext _context,
            IStringLocalizer<ApiController> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<ApiController>();
            _dbContext = _context;
            _localizer = localizer;
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
    }
}