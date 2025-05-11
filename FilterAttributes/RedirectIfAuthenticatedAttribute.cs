using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace dotnetprojekt.FilterAttributes
{
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData["AlertType"] = "info";
                    controller.TempData["AlertMessage"] = "You're already logged in!";
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                }
            }
            base.OnActionExecuting(context);
        }
    }
}

