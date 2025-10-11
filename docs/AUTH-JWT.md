# Authentication, Authorization, and DDD/CQRS Structure

This document explains the technical design for login, JWT issuance, protected endpoints, and how this fits domain-driven design (DDD) and CQRS in this repo.

## Components by Layer

- Application
  - `src/Orbit.Application/Auth/Contracts.cs`
    - `IAuthService`: Login use case
    - `ITokenService`: Abstraction for issuing tokens
    - `IPasswordHasher`: Abstraction for hashing/verifying passwords
    - `AuthTokenDto`: Returned token payload
  - `src/Orbit.Application/Auth/AuthService.cs`
    - Implements `LoginAsync`: loads user, verifies password, loads roles, issues JWT via `ITokenService`
  - `src/Orbit.Application/Users/Specifications/UserByUsernameWithRolesSpec.cs`
    - Brings back `User` aggregate with link collection (`User.Roles`) eagerly loaded

- Infrastructure
  - `src/Orbit.Infrastructure/Security/PasswordHasher.cs`
    - PBKDF2-based hashing with salt and iterations (recommend per-environment iteration tuning)
  - `src/Orbit.Infrastructure/Security/TokenService.cs`
    - Issues JWT using `JwtOptions` (Issuer, Audience, SigningKey, AccessTokenMinutes)
  - `src/Orbit.Infrastructure/Persistence/Entities/UserCredential.cs`
    - `UserCredentialEntity` EF model and `UserCredentialStore` implementing `IUserCredentialStore`
  - `src/Orbit.Infrastructure/Persistence/Configurations/UserCredentialConfiguration.cs`
    - Table configuration for `UserCredentials`
  - `src/Orbit.Infrastructure/DependencyInjection.cs`
    - `AddJwt(Action<JwtOptions>)`, registers `ITokenService`, `IPasswordHasher`, `IUserCredentialStore`

- Web
  - `src/Orbit.Web/Program.cs`
    - Configures JWT Bearer authentication and authorization
    - Endpoints:
      - `POST /api/auth/login` → returns `AuthTokenDto`
      - `POST /api/auth/register` → dev-only sample to create a user with password
      - Protected endpoints (`/api/users`, `/api/users/search`, `POST /api/users`) require authorization

## Token Content and Strategy

- Claims included: `sub` (user id), `unique_name` (username), `email`, `iat`, and role claims (`ClaimTypes.Role` for role names)
- Permissions are intentionally not embedded in the access token by default
  - Reason: permissions can change frequently; embedding them in long-lived tokens risks stale authorization
  - Options: add permissions only with short-lived access tokens; or check permissions server-side (DB/cache/policy)

## DDD and CQRS Rationale

- Repository boundaries
  - Repositories are defined only for aggregate roots: `IReadRepository<TAggregate, TId>` / `IWriteRepository<TAggregate, TId>`
  - Link entities like `UserRole` are not aggregate roots; do not create repositories for them
  - Instead, load via the owning aggregate (`User`) using specifications (e.g., `UserByUsernameWithRolesSpec`)

- Application orchestrates use cases
  - Authentication is an application concern (`IAuthService`), not a domain concern
  - Persistence uses repository abstractions; implementations live in Infrastructure
  - Web/host calls Application services and returns DTOs (not domain entities)

## Configuration

- appsettings.json
  - `Jwt: Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes`
- Program.cs
  - `builder.Services.AddJwt(o => builder.Configuration.GetSection("Jwt").Bind(o));`
  - `.AddAuthentication().AddJwtBearer(...)` with validation params matching the options

## End-to-End Flow

1. Register (dev sample): `POST /api/auth/register { username, email, password }`
   - Creates `User`, hashes password via `IPasswordHasher`, stores in `UserCredentials`
2. Login: `POST /api/auth/login { username, password }`
   - Loads user with roles, verifies password, issues JWT, returns `AuthTokenDto`
3. Call protected endpoints with `Authorization: Bearer <token>`

