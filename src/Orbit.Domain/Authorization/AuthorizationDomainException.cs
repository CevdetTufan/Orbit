namespace Orbit.Domain.Authorization;

/// <summary>
/// Exception thrown when a business rule is violated in the Authorization domain.
/// Following DDD principles, this exception represents domain-specific business rule violations.
/// </summary>
public class AuthorizationDomainException : InvalidOperationException
{
    public AuthorizationDomainException(string message) : base(message)
    {
    }

    public AuthorizationDomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}