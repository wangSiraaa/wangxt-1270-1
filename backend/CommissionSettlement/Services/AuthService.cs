using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommissionSettlement.Data;
using CommissionSettlement.Dtos;
using CommissionSettlement.Enums;
using CommissionSettlement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CommissionSettlement.Services;

public interface IAuthService
{
    Task<UserLoginResponseDto?> LoginAsync(UserLoginDto dto, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserDto>> GetUsersByRoleAsync(UserRole? role, CancellationToken cancellationToken = default);
    Task<List<UserDto>> GetTeamMembersAsync(Guid supervisorId, CancellationToken cancellationToken = default);
    Task SeedDemoDataAsync(CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<UserLoginResponseDto?> LoginAsync(UserLoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.ParentUser)
            .FirstOrDefaultAsync(u => u.UserCode == dto.UserCode && u.IsActive, cancellationToken);

        if (user == null) return null;

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        return new UserLoginResponseDto(
            user.UserId,
            user.UserCode,
            user.UserName,
            user.Role,
            token,
            expiresAt
        );
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("UserCode", user.UserCode)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.ParentUser)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetUsersByRoleAsync(UserRole? role, CancellationToken cancellationToken = default)
    {
        var q = _context.Users.Include(u => u.ParentUser).AsQueryable();
        if (role.HasValue)
            q = q.Where(u => u.Role == role.Value);

        var users = await q.OrderBy(u => u.UserCode).ToListAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<List<UserDto>> GetTeamMembersAsync(Guid supervisorId, CancellationToken cancellationToken = default)
    {
        var members = await _context.Users
            .Include(u => u.ParentUser)
            .Where(u => u.ParentUserId == supervisorId || u.UserId == supervisorId)
            .OrderBy(u => u.UserCode)
            .ToListAsync(cancellationToken);

        return members.Select(MapToDto).ToList();
    }

    public async Task SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
            return;

        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var financeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var supervisorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var agent1Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var agent2Id = Guid.Parse("55555555-5555-5555-5555-555555555555");

        _context.Users.AddRange(
            new User { UserId = adminId, UserCode = "admin", UserName = "系统管理员", Role = UserRole.Admin, IsActive = true },
            new User { UserId = financeId, UserCode = "finance01", UserName = "财务张经理", Role = UserRole.Finance, IsActive = true },
            new User { UserId = supervisorId, UserCode = "sup01", UserName = "李主管", Role = UserRole.Supervisor, IsActive = true },
            new User { UserId = agent1Id, UserCode = "agent01", UserName = "业务员小王", Role = UserRole.Agent, ParentUserId = supervisorId, IsActive = true },
            new User { UserId = agent2Id, UserCode = "agent02", UserName = "业务员小李", Role = UserRole.Agent, ParentUserId = supervisorId, IsActive = true }
        );

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UserDto MapToDto(User u) => new(
        u.UserId,
        u.UserCode,
        u.UserName,
        u.Role,
        u.ParentUserId,
        u.ParentUser?.UserName
    );
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; }
}
