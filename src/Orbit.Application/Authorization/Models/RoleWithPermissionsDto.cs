namespace Orbit.Application.Authorization.Models;

public sealed record RoleWithPermissionsDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<PermissionDto> AssignedPermissions,
    IReadOnlyList<PermissionDto> AvailablePermissions
);