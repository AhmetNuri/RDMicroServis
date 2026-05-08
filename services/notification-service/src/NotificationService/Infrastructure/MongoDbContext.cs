using MongoDB.Driver;
using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDB__ConnectionString"] ?? configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB__DatabaseName"] ?? configuration["MongoDB:DatabaseName"] ?? "notifications";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        EnsureIndexes();
    }

    public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("notifications");

    private void EnsureIndexes()
    {
        var indexKeys = Builders<Notification>.IndexKeys.Ascending(n => n.UserId).Descending(n => n.CreatedAt);
        Notifications.Indexes.CreateOne(new CreateIndexModel<Notification>(indexKeys));

        var unreadIndex = Builders<Notification>.IndexKeys.Combine(
            Builders<Notification>.IndexKeys.Ascending(n => n.UserId),
            Builders<Notification>.IndexKeys.Ascending(n => n.IsRead));
        Notifications.Indexes.CreateOne(new CreateIndexModel<Notification>(unreadIndex));
    }
}
