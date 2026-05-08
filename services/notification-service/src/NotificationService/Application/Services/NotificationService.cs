using MongoDB.Driver;
using NotificationService.Application.DTOs;
using NotificationService.Domain;
using NotificationService.Infrastructure;

namespace NotificationService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly MongoDbContext _mongoContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(MongoDbContext mongoContext, ILogger<NotificationService> logger)
    {
        _mongoContext = mongoContext;
        _logger = logger;
    }

    public async Task<Notification> CreateNotificationAsync(SendNotificationRequest request)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            Metadata = request.Metadata
        };

        await _mongoContext.Notifications.InsertOneAsync(notification);
        _logger.LogInformation("Notification created for user {UserId}: {Title}", request.UserId, request.Title);
        return notification;
    }

    public async Task<(IList<Notification> Items, long Total)> GetUserNotificationsAsync(Guid userId, int page, int pageSize)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
        var total = await _mongoContext.Notifications.CountDocumentsAsync(filter);
        var items = await _mongoContext.Notifications
            .Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow);
        var result = await _mongoContext.Notifications.UpdateOneAsync(
            Builders<Notification>.Filter.Eq(n => n.Id, id), update);
        return result.ModifiedCount > 0;
    }

    public async Task<long> GetUnreadCountAsync(Guid userId)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.UserId, userId),
            Builders<Notification>.Filter.Eq(n => n.IsRead, false));
        return await _mongoContext.Notifications.CountDocumentsAsync(filter);
    }
}
