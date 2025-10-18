namespace Orbit.Application.Users.Models;

public sealed record UserDto(Guid Id, string Username, string Email, bool IsActive);
