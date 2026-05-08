using AuditService.Application.DTOs;
using AuditService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditService.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin,Operator")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("events")]
    public async Task<IActionResult> RecordEvent([FromBody] AuditEventDto dto)
    {
        dto.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        dto.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        await _auditService.RecordEventAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = dto.EventId }, dto);
    }

    [HttpGet("events")]
    public async Task<IActionResult> Search([FromQuery] AuditSearchRequest request)
    {
        var (items, total) = await _auditService.SearchAsync(request);
        return Ok(new { data = items, total, page = request.Page, size = request.Size });
    }

    [HttpGet("events/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var e = await _auditService.GetByIdAsync(id);
        return e == null ? NotFound() : Ok(e);
    }

    [HttpGet("events/user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var items = await _auditService.GetByUserIdAsync(userId, page, size);
        return Ok(new { data = items, page, size });
    }
}
