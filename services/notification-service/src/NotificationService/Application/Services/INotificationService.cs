using NotificationService.Application.DTOs;
using NotificationService.Domain;

namespace NotificationService.Application.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(SendNotificationRequest request);
    Task<(IList<Notification> Items, long Total)> GetUserNotificationsAsync(Guid userId, int page, int pageSize);
    Task<bool> MarkAsReadAsync(string id);
    Task<long> GetUnreadCountAsync(Guid userId);
}
