using AuditService.Application.DTOs;
using AuditService.Domain;
using AuditService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Application.Services;

public class AuditService : IAuditService
{
    private readonly AuditDbContext _dbContext;
    private readonly ElasticsearchService _esService;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AuditDbContext dbContext, ElasticsearchService esService, ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _esService = esService;
        _logger = logger;
    }

    public async Task RecordEventAsync(AuditEventDto dto)
    {
        var auditEvent = new AuditEvent
        {
            EventId = dto.EventId == Guid.Empty ? Guid.NewGuid() : dto.EventId,
            EventType = dto.EventType,
            UserId = dto.UserId,
            ServiceName = dto.ServiceName,
            Action = dto.Action,
            ResourceType = dto.ResourceType,
            ResourceId = dto.ResourceId,
            OldValue = dto.OldValue,
            NewValue = dto.NewValue,
            IpAddress = dto.IpAddress,
            CorrelationId = dto.CorrelationId,
            Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp,
            Metadata = dto.Metadata
        };

        var pgEvent = new AuditEventPg
        {
            EventId = auditEvent.EventId,
            EventType = auditEvent.EventType,
            UserId = auditEvent.UserId,
            ServiceName = auditEvent.ServiceName,
            Action = auditEvent.Action,
            ResourceType = auditEvent.ResourceType,
            ResourceId = auditEvent.ResourceId,
            OldValue = auditEvent.OldValue,
            NewValue = auditEvent.NewValue,
            IpAddress = auditEvent.IpAddress,
            CorrelationId = auditEvent.CorrelationId,
            Timestamp = auditEvent.Timestamp
        };

        _dbContext.AuditEvents.Add(pgEvent);
        await _dbContext.SaveChangesAsync();

        try
        {
            await _esService.IndexAsync(auditEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index audit event {EventId} in Elasticsearch", auditEvent.EventId);
        }

        _logger.LogInformation("Audit event recorded: {EventType} on {ResourceType}/{ResourceId}",
            auditEvent.EventType, auditEvent.ResourceType, auditEvent.ResourceId);
    }

    public async Task<(IList<AuditEventDto> Items, long Total)> SearchAsync(AuditSearchRequest request)
    {
        try
        {
            return await _esService.SearchAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch search failed, falling back to PostgreSQL");
            return await SearchPgAsync(request);
        }
    }

    private async Task<(IList<AuditEventDto> Items, long Total)> SearchPgAsync(AuditSearchRequest request)
    {
        var query = _dbContext.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var tsQuery = EF.Functions.ToTsQuery("english", request.Query);
            query = query.Where(e => e.SearchVector!.Matches(tsQuery));
        }
        if (request.UserId.HasValue)
            query = query.Where(e => e.UserId == request.UserId.Value);
        if (!string.IsNullOrWhiteSpace(request.ServiceName))
            query = query.Where(e => e.ServiceName == request.ServiceName);
        if (request.From.HasValue)
            query = query.Where(e => e.Timestamp >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(e => e.Timestamp <= request.To.Value);

        var total = await query.LongCountAsync();
        var items = await query.OrderByDescending(e => e.Timestamp)
            .Skip((request.Page - 1) * request.Size).Take(request.Size)
            .Select(e => MapToDto(e)).ToListAsync();

        return (items, total);
    }

    public async Task<AuditEventDto?> GetByIdAsync(Guid eventId)
    {
        var e = await _dbContext.AuditEvents.FindAsync(eventId);
        return e == null ? null : MapToDto(e);
    }

    public async Task<IList<AuditEventDto>> GetByUserIdAsync(Guid userId, int page, int size)
    {
        return await _dbContext.AuditEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * size).Take(size)
            .Select(e => MapToDto(e))
            .ToListAsync();
    }

    private static AuditEventDto MapToDto(AuditEventPg e) => new()
    {
        EventId = e.EventId,
        EventType = e.EventType,
        UserId = e.UserId,
        ServiceName = e.ServiceName,
        Action = e.Action,
        ResourceType = e.ResourceType,
        ResourceId = e.ResourceId,
        OldValue = e.OldValue,
        NewValue = e.NewValue,
        IpAddress = e.IpAddress,
        CorrelationId = e.CorrelationId,
        Timestamp = e.Timestamp
    };
}
