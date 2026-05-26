using sfa_api.Features.Notifications.Repositories;
using sfa_api.Features.Notifications.Services;

namespace sfa_api.Features.Notifications;

public static class NotificationsServiceExtensions
{
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationHistoryService, NotificationHistoryService>();
        return services;
    }
}
