namespace Orbit.Domain.Users;

/// <summary>
/// Exception thrown when a business rule is violated in the Users domain.
/// Following DDD principles, this exception represents domain-specific business rule violations.
/// </summary>
public class UsersDomainException : InvalidOperationException
{
    public UsersDomainException(string message) : base(message)
    {
    }

    public UsersDomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
