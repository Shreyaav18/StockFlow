using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StockFlow.Web.Controllers
{
    [AllowAnonymous]
    public class ErrorController : BaseController
    {
        [HttpGet]
        [Route("Error")]
        public IActionResult Index(int? code, string? message)
        {
            ViewBag.StatusCode = code ?? 500;
            ViewBag.Message = message ?? "An unexpected error occurred.";

            ViewBag.Title = code switch
            {
                400 => "Bad Request",
                401 => "Unauthorised",
                403 => "Forbidden",
                404 => "Not Found",
                422 => "Validation Error",
                429 => "Too Many Requests",
                _ => "Server Error"
            };

            return View();
        }

        [HttpGet]
        [Route("Error/NotFound")]
        public IActionResult NotFound404()
        {
            ViewBag.StatusCode = 404;
            ViewBag.Title = "Not Found";
            ViewBag.Message = "The page you are looking for does not exist.";
            return View("Index");
        }
    }
}