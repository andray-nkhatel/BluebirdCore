namespace BluebirdCore.Middleware
{
    namespace SchoolManagementSystem.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                message = "An error occurred while processing your request.",
                details = exception.Message
            };

            switch (exception)
            {
                case ArgumentException:
                    context.Response.StatusCode = 400;
                    break;
                case UnauthorizedAccessException:
                    context.Response.StatusCode = 401;
                    break;
                case KeyNotFoundException:
                    context.Response.StatusCode = 404;
                    break;
                default:
                    context.Response.StatusCode = 500;
                    break;
            }

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
}