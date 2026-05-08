using AuditService.Application.DTOs;

namespace AuditService.Application.Services;

public interface IAuditService
{
    Task RecordEventAsync(AuditEventDto dto);
    Task<(IList<AuditEventDto> Items, long Total)> SearchAsync(AuditSearchRequest request);
    Task<AuditEventDto?> GetByIdAsync(Guid eventId);
    Task<IList<AuditEventDto>> GetByUserIdAsync(Guid userId, int page, int size);
}
