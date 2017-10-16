﻿using AiursoftBase.Exceptions;
using AiursoftBase.Models;
using AiursoftBase.Models.API.UserAddressModels;
using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Attributes
{
    public class ForceValidateAccessToken : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            var controller = context.Controller as UserController;
            var accessToken = context.HttpContext.Request.Query[nameof(WithAccessTokenAddressModel.AccessToken)].ToString();
            var target = controller._dbContext
                .AccessToken
                .SingleOrDefault(t => t.Value == accessToken);

            if (target == null)
            {
                var arg = new AiurProtocal
                {
                    code = ErrorType.Unauthorized,
                    message = "We can not validate your access token!"
                };
                context.Result = new JsonResult(arg);
            }
            else if (!target.IsAlive)
            {
                var arg = new AiurProtocal
                {
                    code = ErrorType.Unauthorized,
                    message = "Your access token is already Timeout!"
                };
                context.Result = new JsonResult(arg);
            }
        }
    }
}
