using Serilog.Context;

namespace StockFlow.Web.Common
{
    public class CorrelationIdMiddleware
    {
        private const string CORRELATION_HEADER = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[CORRELATION_HEADER].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Response.Headers[CORRELATION_HEADER] = correlationId;
            context.Items[CORRELATION_HEADER] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}