using CommissionSettlement.Common;
using CommissionSettlement.Dtos;
using CommissionSettlement.Enums;
using CommissionSettlement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommissionSettlement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResponse<UserLoginResponseDto>> Login([FromBody] UserLoginDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        if (result == null)
            return ApiResponse<UserLoginResponseDto>.Fail("用户名或密码错误");

        return ApiResponse<UserLoginResponseDto>.Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        var user = await _authService.GetUserByIdAsync(userId, cancellationToken);
        return user == null
            ? ApiResponse<UserDto>.Fail("用户不存在")
            : ApiResponse<UserDto>.Ok(user);
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin,Supervisor,Finance")]
    public async Task<ApiResponse<List<UserDto>>> GetUsers([FromQuery] UserRole? role, CancellationToken cancellationToken)
    {
        var users = await _authService.GetUsersByRoleAsync(role, cancellationToken);
        return ApiResponse<List<UserDto>>.Ok(users);
    }

    [HttpGet("team-members")]
    [Authorize(Roles = "Supervisor,Admin")]
    public async Task<ApiResponse<List<UserDto>>> GetTeamMembers(CancellationToken cancellationToken)
    {
        var supervisorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        var members = await _authService.GetTeamMembersAsync(supervisorId, cancellationToken);
        return ApiResponse<List<UserDto>>.Ok(members);
    }

    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<ApiResponse<bool>> SeedDemoData(CancellationToken cancellationToken)
    {
        await _authService.SeedDemoDataAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true);
    }
}
