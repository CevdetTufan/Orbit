namespace Orbit.Application.Auth.Models;
public sealed class LoginAttemptListItemDto
{
	public Guid Id { get; init; }
	public string Username { get; init; } = string.Empty;
	public Guid? UserId { get; init; }
	public DateTime AttemptedAtUtc { get; init; }
	public bool IsSuccessful { get; init; }
	public string? RemoteIp { get; init; }
	public string? UserAgent { get; init; }
}
