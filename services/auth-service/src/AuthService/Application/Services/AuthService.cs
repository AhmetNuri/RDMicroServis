using AuthService.Application.DTOs;
using AuthService.Domain;
using AuthService.Infrastructure;

namespace AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly AuthDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        AuthDbContext dbContext,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userRepository.FindByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Status = UserStatus.Active
        };

        await _userRepository.AddAsync(user);

        // Assign default Customer role
        var customerRole = _dbContext.Roles.FirstOrDefault(r => r.Name == "Customer");
        if (customerRole != null)
        {
            _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = customerRole.Id });
            await _dbContext.SaveChangesAsync();
        }

        var roles = await _userRepository.GetRolesByUserIdAsync(user.Id);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        _logger.LogInformation("User registered: {Email}", user.Email);

        return BuildAuthResponse(user, roles, accessToken, refreshToken.Token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.Status == UserStatus.Locked)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                throw new UnauthorizedAccessException($"Account locked until {user.LockoutEnd.Value:u}.");

            user.Status = UserStatus.Active;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.Status = UserStatus.Locked;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("Account locked: {Email}", user.Email);
            }
            await _userRepository.UpdateAsync(user);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _userRepository.UpdateAsync(user);

        var roles = await _userRepository.GetRolesByUserIdAsync(user.Id);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return BuildAuthResponse(user, roles, accessToken, refreshToken.Token);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var token = await _userRepository.FindRefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired or revoked.");

        token.IsRevoked = true;
        _dbContext.RefreshTokens.Update(token);

        var user = await _userRepository.FindByIdAsync(token.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var roles = await _userRepository.GetRolesByUserIdAsync(user.Id);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

        await _dbContext.SaveChangesAsync();

        return BuildAuthResponse(user, roles, accessToken, newRefreshToken.Token);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _userRepository.FindRefreshTokenAsync(refreshToken);
        if (token != null)
        {
            token.IsRevoked = true;
            _dbContext.RefreshTokens.Update(token);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        var roles = await _userRepository.GetRolesByUserIdAsync(userId);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Status = user.Status,
            Roles = roles.ToArray()
        };
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshExpiresIn = int.Parse(_configuration["Jwt:RefreshExpiresIn"] ?? "86400");
        var token = new RefreshToken
        {
            UserId = userId,
            Token = _tokenService.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddSeconds(refreshExpiresIn)
        };
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();
        return token;
    }

    private static AuthResponse BuildAuthResponse(User user, IList<string> roles, string accessToken, string refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status,
                Roles = roles.ToArray()
            }
        };
    }
}
