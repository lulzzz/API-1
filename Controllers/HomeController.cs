using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.Extensions.Logging;
using API.Services;
using API.Models;
using Microsoft.AspNetCore.Identity;
using AiursoftBase.Models;
using API.Models.HomeViewModels;
using AiursoftBase;
using AiursoftBase.Attributes;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [AiurRequireHttps]
    public class HomeController : AiurController
    {
        private readonly UserManager<APIUser> _userManager;
        private readonly SignInManager<APIUser> _signInManager;
        private readonly ILogger _logger;
        private readonly APIDbContext _dbContext;
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(
            UserManager<APIUser> userManager,
            SignInManager<APIUser> signInManager,
            ILoggerFactory loggerFactory,
            APIDbContext _context,
            IStringLocalizer<HomeController> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<HomeController>();
            _dbContext = _context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var cuser = await GetCurrentUserAsync();
            return Json(new IndexViewModel
            {
                Signedin = User.Identity.IsAuthenticated,
                UserId = cuser?.Id,
                ServerTime = DateTime.Now,
                UserName = cuser?.NickName,
                UserImage = cuser?.HeadImgUrl,
                code  = ErrorType.Success,
                message = "Server started successfully!",
                Local = _localizer["en"]
            });
        }
        private async Task<APIUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(HttpContext.User);
        }
    }
}