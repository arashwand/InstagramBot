using System.Security.Claims;

namespace InstagramBot.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var request = context.Request;
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            try
            {
                await _next(context);

                var duration = DateTime.UtcNow - startTime;
                var response = context.Response;

                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms for user {UserId} from {IpAddress}",
                    request.Method,
                    request.Path,
                    response.StatusCode,
                    duration.TotalMilliseconds,
                    userId,
                    ipAddress);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;

                _logger.LogError(ex,
                    "HTTP {Method} {Path} failed in {Duration}ms for user {UserId} from {IpAddress}",
                    request.Method,
                    request.Path,
                    duration.TotalMilliseconds,
                    userId,
                    ipAddress);

                throw;
            }
        }
    }
}