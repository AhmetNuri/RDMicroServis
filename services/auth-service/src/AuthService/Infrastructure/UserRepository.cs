using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<IList<string>> GetRolesByUserIdAsync(Guid userId);
    Task<RefreshToken?> FindRefreshTokenAsync(string token);
}

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _dbContext;

    public UserRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> FindByEmailAsync(string email) =>
        await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> FindByIdAsync(Guid id) =>
        await _dbContext.Users.FindAsync(id);

    public async Task AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IList<string>> GetRolesByUserIdAsync(Guid userId) =>
        await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

    public async Task<RefreshToken?> FindRefreshTokenAsync(string token) =>
        await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
}
