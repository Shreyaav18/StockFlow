using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StockFlow.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        protected int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        protected string CurrentUserRole =>
            User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        protected string CurrentUserName =>
            User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        protected IActionResult JsonSuccess(object? data = null, string message = "Success")
        {
            return Json(new { success = true, message, data });
        }

        protected IActionResult JsonFail(string message, int statusCode = 400)
        {
            Response.StatusCode = statusCode;
            return Json(new { success = false, message });
        }

        protected void SetSuccessMessage(string message)
            => TempData["SuccessMessage"] = message;

        protected void SetErrorMessage(string message)
            => TempData["ErrorMessage"] = message;
    }
}