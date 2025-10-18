namespace Orbit.Application.Users.Models;

public sealed record UserDetailDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    IReadOnlyList<RoleInfo> Roles
);
