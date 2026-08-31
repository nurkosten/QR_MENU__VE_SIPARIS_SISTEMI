namespace RestaurantMenu.WebUI.Infrastructure;

public sealed class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;
    private readonly IActivityLogQueue _logs;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger, IActivityLogQueue logs)
    {
        _next = next;
        _logger = logger;
        _logs = logs;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmeyen hata: {Method} {Path}", context.Request.Method, context.Request.Path);
            int? restaurantId = null;
            try
            {
                if (context.Session.IsAvailable)
                {
                    restaurantId = context.Session.GetInt32(ICurrentRestaurant.SessionKey);
                }
            }
            catch (InvalidOperationException)
            {
            }

            _logs.Enqueue(new RestaurantMenu.Business.Dtos.ActivityLogEntry
            {
                Level = "Error",
                Category = "Hata",
                Message = ex.Message,
                UserName = context.User.Identity?.Name,
                Path = context.Request.Path.Value,
                HttpMethod = context.Request.Method,
                StatusCode = 500,
                RestaurantId = restaurantId
            });
            throw;
        }
    }
}
