namespace AuditService.Application.DTOs;

public class AuditSearchRequest
{
    public string? Query { get; set; }
    public Guid? UserId { get; set; }
    public string? ServiceName { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
