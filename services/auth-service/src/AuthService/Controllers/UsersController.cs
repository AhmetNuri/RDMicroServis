using AuthService.Domain;
using AuthService.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AuthDbContext dbContext, ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.Users.AsNoTracking();
        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id, u.Email, u.FirstName, u.LastName, u.Status, u.CreatedAt,
                Roles = _dbContext.UserRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync();

        return Ok(new { data = users, total, page, pageSize });
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateRolesRequest request)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        var existingRoles = _dbContext.UserRoles.Where(ur => ur.UserId == id);
        _dbContext.UserRoles.RemoveRange(existingRoles);

        var roles = await _dbContext.Roles.Where(r => request.RoleNames.Contains(r.Name)).ToListAsync();
        foreach (var role in roles)
        {
            _dbContext.UserRoles.Add(new UserRole { UserId = id, RoleId = role.Id });
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Roles updated for user {UserId}: {Roles}", id, string.Join(", ", request.RoleNames));
        return Ok(new { message = "Roles updated" });
    }
}

public class UpdateRolesRequest
{
    public List<string> RoleNames { get; set; } = [];
}
