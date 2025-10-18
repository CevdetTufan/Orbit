namespace Orbit.Application.Authorization.Models;

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string? Description
);