namespace Orbit.Application.Authorization.Models;

public sealed record RoleDto(
	Guid Id, 
	string Name, 
	string? Description, 
	bool CanDelete);
