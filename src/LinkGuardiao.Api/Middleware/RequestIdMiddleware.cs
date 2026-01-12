using Serilog.Context;

namespace LinkGuardiao.Api.Middleware
{
    public class RequestIdMiddleware
    {
        private const string HeaderName = "X-Request-Id";
        private readonly RequestDelegate _next;

        public RequestIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
                && !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString("N");

            context.TraceIdentifier = requestId;
            context.Response.Headers[HeaderName] = requestId;

            using (LogContext.PushProperty("RequestId", requestId))
            {
                await _next(context);
            }
        }
    }
}
