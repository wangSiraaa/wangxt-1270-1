using CommissionSettlement.Enums;

namespace CommissionSettlement.Dtos;

public record UserDto(
    Guid UserId,
    string UserCode,
    string UserName,
    UserRole Role,
    Guid? ParentUserId,
    string? ParentUserName,
    bool IsActive
);

public record UserLoginDto(
    string UserCode,
    string Password
);

public record UserLoginResponseDto(
    Guid UserId,
    string UserCode,
    string UserName,
    UserRole Role,
    string Token,
    DateTime ExpiresAt
);
