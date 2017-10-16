using AiursoftBase.Exceptions;
using AiursoftBase.Models.API.UserAddressModels;
using API.Controllers;
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
                throw new WrongAccessTokenException();
            }
            else if (!target.IsAlive)
            {
                throw new TimeOutAccessTokenException();
            }
        }
    }
}
