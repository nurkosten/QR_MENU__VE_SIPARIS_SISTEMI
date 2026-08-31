using Hangfire;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Dtos;

namespace RestaurantMenu.WebUI.Infrastructure;

public interface IActivityLogQueue
{
    void Enqueue(ActivityLogEntry entry);
}

public sealed class HangfireActivityLogQueue : IActivityLogQueue
{
    public void Enqueue(ActivityLogEntry entry)
    {
        try
        {
            BackgroundJob.Enqueue<IActivityLogService>(service => service.AddAsync(entry, CancellationToken.None));
        }
        catch
        {
            // Log yazımı istek akışını kesmesin.
        }
    }
}
