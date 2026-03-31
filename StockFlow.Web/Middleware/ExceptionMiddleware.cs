using System.Net;
using System.Text.Json;
using Serilog;
using StockFlow.Web.Exceptions;

namespace StockFlow.Web.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                Log.Warning(ex, "Application exception on {Method} {Path} — {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await HandleExceptionAsync(context, ex.StatusCode, ex.Message, ex.Details);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception on {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await HandleExceptionAsync(
                    context,
                    HttpStatusCode.InternalServerError,
                    ErrorMessages.General.ServerError
                );
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string message,
            string? details = null)
        {
            if (context.Response.HasStarted)
            {
                Log.Warning("Response already started — cannot modify headers for error {StatusCode}", statusCode);
                return;
            }

            if (IsApiRequest(context))
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)statusCode;

                var response = new
                {
                    success = false,
                    statusCode = (int)statusCode,
                    message,
                    details
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
                );
            }
            else
            {
                context.Response.StatusCode = (int)statusCode;
                context.Response.Redirect(
                    $"/Error?code={(int)statusCode}&message={Uri.EscapeDataString(message)}"
                );
            }
        }

        private static bool IsApiRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/api") ||
                   context.Request.Headers["Accept"].ToString().Contains("application/json");
        }
    }
}