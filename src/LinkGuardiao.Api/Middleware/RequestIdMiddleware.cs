using Serilog.Context;

namespace LinkGuardiao.Api.Middleware
{
    public class RequestIdMiddleware
    {
        private const string HeaderName = "X-Request-Id";
        private const int MaxRequestIdLength = 64;
        private readonly RequestDelegate _next;

        public RequestIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
                && !string.IsNullOrWhiteSpace(headerValue)
                ? Sanitize(headerValue.ToString())
                : Guid.NewGuid().ToString("N");

            context.TraceIdentifier = requestId;
            context.Response.Headers[HeaderName] = requestId;

            using (LogContext.PushProperty("RequestId", requestId))
            {
                await _next(context);
            }
        }

        // Prevent log injection by allowing only safe characters and limiting length.
        private static string Sanitize(string value)
        {
            var sanitized = new string(
                value.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
                     .Take(MaxRequestIdLength)
                     .ToArray());

            return sanitized.Length > 0 ? sanitized : Guid.NewGuid().ToString("N");
        }
    }
}
