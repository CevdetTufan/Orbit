# Orbit

Domain-driven starter with Authorization and User aggregates, a web frontend, and unit tests.

**Highlights**
- Clean domain model (entities, value objects, aggregates)
- Authorization via `Role`, `Permission`, and link entities
- Minimal ASP.NET Core frontend in `src/Orbit.Web`
- xUnit test project in `tests/Orbit.Domain.Test`
- **Domain Events** for event-driven architecture
- **Real-time session management** with Blazor Server

**Solution Structure**
- `src/Orbit.Domain` — Core domain model (no infrastructure dependencies)
- `src/Orbit.Application` — Application layer (use cases, orchestration, event handlers)
- `src/Orbit.Infrastructure` — Infrastructure concerns (persistence, event dispatching)
- `src/Orbit.Web` — Web application (UI/host, Blazor Server)
- `tests/Orbit.Domain.Test` — Unit tests for the domain
- `docs/` — Technical documentation

**Documentation**
- **[Domain Events and Session Management](docs/domain-events-session-management.md)** *(Türkçe)*
  - Domain Events pattern implementation
  - Event dispatching mechanism
  - Blazor Server circuit tracking
  - Real-time user logout on deactivation
  - Complete architecture and flow diagrams

**Requirements**
- .NET SDK `9.0+`
- SQL Server (LocalDB or full version)

**Restore, Build, Run**
- Restore: `dotnet restore`
- Build: `dotnet build --configuration Release`
- Run web app: `dotnet run --project src/Orbit.Web`

**Run Tests**
- All tests: `dotnet test`
- Domain tests only: `dotnet test tests/Orbit.Domain.Test/Orbit.Domain.Test.csproj`

The domain tests cover value objects (`Email`, `Username`), authorization (`Permission`, `Role`, link entities), and `User` aggregate behaviors (activation, role assignment, idempotency, and removals).

**Key Domain Types**
- `src/Orbit.Domain/Common/Entity.cs` — Base entity with identity, equality, and domain events
- `src/Orbit.Domain/Common/IDomainEvent.cs` — Marker interface for domain events
- `src/Orbit.Domain/Users/User.cs` — `User` aggregate root with roles and activation
- `src/Orbit.Domain/Users/Events/UserDeactivatedEvent.cs` — Domain event for user deactivation
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
 
For authentication/authorization and the DDD/CQRS repository design used here, see docs/AUTH-JWT.md for a detailed walkthrough.

**DDD Repositories & Specifications**
- Read/Write ayrımı:
  - `src/Orbit.Domain/Common/IReadRepository.cs` — Sadece okuma operasyonları: `GetByIdAsync`, `ListAsync`, `AnyAsync`, `CountAsync`, ayrıca Specification overload'ları.
  - `src/Orbit.Domain/Common/IWriteRepository.cs` — Yazma operasyonları: `AddAsync`, `Update`, `Remove`.
  - `src/Orbit.Domain/Common/IRepository.cs` — Her ikisinin birleşimi (read + write).
- Unit of Work:
  - `src/Orbit.Domain/Common/IUnitOfWork.cs` — `SaveChangesAsync` sözleşmesi.
  - `src/Orbit.Infrastructure/Persistence/UnitOfWork.cs` — `AppDbContext` üstünden implementasyon; domain event dispatching içerir.
- EF Core implementasyonu:
  - `src/Orbit.Infrastructure/Persistence/Repositories/EfRepository.cs` — Open-generic repository. Okuma tarafı `AsNoTracking()` kullanır, yazma tarafı tracking ile çalışır. Specification destekler.
- Specification Pattern:
  - `src/Orbit.Domain/Common/Specifications/ISpecification.cs` — `Criteria`, `Includes`, `OrderBy/Descending`, `Skip/Take`, `AsNoTracking` içeren soyutlama ve `BaseSpecification<T>`.
  - `src/Orbit.Infrastructure/Persistence/Specifications/SpecificationEvaluator.cs` — Specification'ı EF `IQueryable`'a uygular: `Where`, `Include`, `OrderBy`, `Skip/Take`, `AsNoTracking`.
  - Örnek: `src/Orbit.Application/Users/Specifications/UsersByQuerySpec.cs` — Kullanıcıları metin araması ve sıralama ile filtreler.

**Domain Events**
- Pattern: Event-driven architecture
- Components:
  - `src/Orbit.Application/Common/IDomainEventDispatcher.cs` — Event dispatcher interface
  - `src/Orbit.Application/Common/DomainEventDispatcher.cs` — Reflection-based dispatcher implementation
  - `src/Orbit.Application/Common/IDomainEventHandler.cs` — Generic event handler interface
  - `src/Orbit.Application/Users/EventHandlers/UserDeactivatedEventHandler.cs` — Handles user deactivation events
- Flow: Entity raises event → UnitOfWork collects → SaveChanges commits → Events dispatched → Handlers execute
- Use case: User deactivation triggers automatic session termination across all active circuits

**Blazor Server Session Management**
- `src/Orbit.Web/Services/BlazorUserSessionManager.cs` — Circuit tracking and session termination
- `src/Orbit.Web/Security/CircuitAuthenticationStateProvider.cs` — Authentication state with circuit lifecycle management
- Features:
  - Real-time session tracking
  - Multi-device logout support
  - Callback-based termination notification
  - Thread-safe concurrent dictionary

**Dependency Injection**
- Katman bazlı DI genişletmeleri:
  - `src/Orbit.Application/DependencyInjection.cs` — `AddApplication()`; uygulama katmanı servisleri, domain event dispatcher ve handlers için giriş noktası.
  - `src/Orbit.Infrastructure/DependencyInjection.cs` — `AddInfrastructure(Action<DbContextOptionsBuilder>)`; `AppDbContext`, `IUnitOfWork` ve open-generic repo kayıtları (`IReadRepository<,>`, `IWriteRepository<,>`, `IRepository<,>`) yapılır.

**Web Uygulaması Kablolama**
- `src/Orbit.Web/Program.cs`:
  - `builder.Services.AddApplication();`
  - `builder.Services.AddInfrastructure(options => options.UseSqlServer(...));`
  - `builder.Services.AddSingleton<BlazorUserSessionManager>();`
  - `builder.Services.AddScoped<IUserSessionManager>(...);`
  - Migration'lar otomatik uygulanır.
  - Development seed data (admin/user)

**Veritabanı Sağlayıcı ve Ayarları**
- Web projesi `Microsoft.EntityFrameworkCore.SqlServer` kullanır:
  - `src/Orbit.Web/Orbit.Web.csproj` — paket referansı ve Application/Infrastructure proje referansları ekli.
  - `src/Orbit.Web/appsettings.json` — `ConnectionStrings:Default = Server=(localdb)\\mssqllocaldb;Database=OrbitDb;...`.
  - `src/Orbit.Web/appsettings.Development.json` — Development ortamı için ayarlar.
- Migration yönetimi:
  - Migration ekleme: `dotnet ef migrations add <Name> --project src/Orbit.Infrastructure --startup-project src/Orbit.Web`
  - Uygulama: `dotnet ef database update --project src/Orbit.Infrastructure --startup-project src/Orbit.Web`
  - Not: Uygulama ilk çalıştırıldığında migration'lar otomatik uygulanır.

**Repository Kullanım Örneği**
- Okuma:
  - `IReadRepository<User, Guid>` üzerinden: `await repo.ListAsync(ct);`
  - Specification ile: `await repo.ListAsync(new UsersByQuerySpec("ali"), ct);`
- Yazma:
  - `await writeRepo.AddAsync(user, ct);`
  - `await unitOfWork.SaveChangesAsync(ct);` — Domain events otomatik dispatch edilir

**Tasarım Notları**
- DDD sınırları: Domain katmanı sadece arayüzleri ve specification sözleşmesini içerir; EF Core detayları Infrastructure'da kalır.
- CQRS uyumu: Okuma ve yazma yetenekleri `IReadRepository` ve `IWriteRepository` ile ayrıştırılmıştır; performans için okuma-odaklı `AsNoTracking()` varsayılan kabul edilmiştir.
- Event-driven: Domain events ile loose coupling sağlanır; side-effects (session termination gibi) event handler'larda yönetilir.
- Clean Architecture: Bağımlılıklar her zaman içeri doğru; Domain → Application → Infrastructure → Presentation.
- Genişletilebilirlik: Specification ile sorgular yeniden kullanılabilir, test edilebilir ve Infrastructure'a sızdırılmadan ifade edilebilir.
