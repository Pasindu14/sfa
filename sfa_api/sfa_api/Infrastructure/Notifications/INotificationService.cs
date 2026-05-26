namespace sfa_api.Infrastructure.Notifications;

public interface INotificationService
{
    Task SendToUserAsync(int userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendToDistributorUsersAsync(int distributorId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendToDistributorSalesRepsAsync(int distributorId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
