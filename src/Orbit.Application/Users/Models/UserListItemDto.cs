namespace Orbit.Application.Users.Models;

public sealed record UserListItemDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles
);
