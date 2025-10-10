# Orbit

Domain-driven starter with Authorization and User aggregates, a web frontend, and unit tests.

**Highlights**
- Clean domain model (entities, value objects, aggregates)
- Authorization via `Role`, `Permission`, and link entities
- Minimal ASP.NET Core frontend in `src/Orbit.Web`
- xUnit test project in `tests/Orbit.Domain.Test`

**Solution Structure**
- `src/Orbit.Domain` — Core domain model (no infrastructure dependencies)
- `src/Orbit.Application` — Application layer (use cases, orchestration)
- `src/Orbit.Infrastructure` — Infrastructure concerns (persistence, etc.)
- `src/Orbit.Web` — Web application (UI/host)
- `tests/Orbit.Domain.Test` — Unit tests for the domain

**Requirements**
- .NET SDK `9.0+`

**Restore, Build, Run**
- Restore: `dotnet restore`
- Build: `dotnet build --configuration Release`
- Run web app: `dotnet run --project src/Orbit.Web`

**Run Tests**
- All tests: `dotnet test`
- Domain tests only: `dotnet test tests/Orbit.Domain.Test/Orbit.Domain.Test.csproj`

The domain tests cover value objects (`Email`, `Username`), authorization (`Permission`, `Role`, link entities), and `User` aggregate behaviors (activation, role assignment, idempotency, and removals).

**Key Domain Types**
- `src/Orbit.Domain/Common/Entity.cs` — Base entity with identity and equality
- `src/Orbit.Domain/Users/User.cs` — `User` aggregate root with roles and activation
- `src/Orbit.Domain/Users/ValueObjects/Email.cs` — Simple email guard/trim
- `src/Orbit.Domain/Users/ValueObjects/Username.cs` — Length guard/trim
- `src/Orbit.Domain/Authorization/Permission.cs` — Permission with code validation
- `src/Orbit.Domain/Authorization/Role.cs` — Role with name validation and permissions

**Contributing**
- Keep domain pure: no infrastructure dependencies in `Orbit.Domain`
- Prefer small, focused tests in `tests/Orbit.Domain.Test`
- Follow existing naming and folder structure

**License**
- See `LICENSE.txt`
