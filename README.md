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

**DDD Repositories & Specifications**
- Read/Write ayrımı:
  - `src/Orbit.Domain/Common/IReadRepository.cs` — Sadece okuma operasyonları: `GetByIdAsync`, `ListAsync`, `AnyAsync`, `CountAsync`, ayrıca Specification overload’ları.
  - `src/Orbit.Domain/Common/IWriteRepository.cs` — Yazma operasyonları: `AddAsync`, `Update`, `Remove`.
  - `src/Orbit.Domain/Common/IRepository.cs` — Her ikisinin birleşimi (read + write).
- Unit of Work:
  - `src/Orbit.Domain/Common/IUnitOfWork.cs` — `SaveChangesAsync` sözleşmesi.
  - `src/Orbit.Infrastructure/Persistence/UnitOfWork.cs` — `AppDbContext` üstünden implementasyon.
- EF Core implementasyonu:
  - `src/Orbit.Infrastructure/Persistence/Repositories/EfRepository.cs` — Open-generic repository. Okuma tarafı `AsNoTracking()` kullanır, yazma tarafı tracking ile çalışır. Specification destekler.
- Specification Pattern:
  - `src/Orbit.Domain/Common/Specifications/ISpecification.cs` — `Criteria`, `Includes`, `OrderBy/Descending`, `Skip/Take`, `AsNoTracking` içeren soyutlama ve `BaseSpecification<T>`.
  - `src/Orbit.Infrastructure/Persistence/Specifications/SpecificationEvaluator.cs` — Specification’ı EF `IQueryable`’a uygular: `Where`, `Include`, `OrderBy`, `Skip/Take`, `AsNoTracking`.
  - Örnek: `src/Orbit.Application/Users/Specifications/UsersByQuerySpec.cs` — Kullanıcıları metin araması ve sıralama ile filtreler.

**Dependency Injection**
- Katman bazlı DI genişletmeleri:
  - `src/Orbit.Application/DependencyInjection.cs` — `AddApplication()`; uygulama katmanı servisleri için giriş noktası.
  - `src/Orbit.Infrastructure/DependencyInjection.cs` — `AddInfrastructure(Action<DbContextOptionsBuilder>)`; `AppDbContext`, `IUnitOfWork` ve open-generic repo kayıtları (`IReadRepository<,>`, `IWriteRepository<,>`, `IRepository<,>`) yapılır.

**Web Uygulaması Kablolama**
- `src/Orbit.Web/Program.cs`:
  - `builder.Services.AddApplication();`
  - `builder.Services.AddInfrastructure(options => options.UseSqlite(builder.Configuration.GetConnectionString("Default")));`
  - Demo amaçlı `EnsureCreated()` çağrısı ile veritabanı oluşturulur.
  - Minimal API uçları:
    - `GET /api/users` — Tüm kullanıcıları listeler (read repository, no-tracking).
    - `POST /api/users?username=...&email=...` — Yeni kullanıcı ekler ve `IUnitOfWork.SaveChangesAsync()` ile kaydeder.
    - `GET /api/users/search?q=...` — `UsersByQuerySpec` specification ile arama yapar.

**Veritabanı Sağlayıcı ve Ayarları**
- Web projesi `Microsoft.EntityFrameworkCore.Sqlite` kullanır:
  - `src/Orbit.Web/Orbit.Web.csproj` — paket referansı ve Application/Infrastructure proje referansları ekli.
  - `src/Orbit.Web/appsettings.json` — `ConnectionStrings:Default = Data Source=orbit.db`.
  - `src/Orbit.Web/appsettings.Development.json` — `ConnectionStrings:Default = Data Source=orbit.dev.db`.
- Not: Üretimde `EnsureCreated()` yerine migration akışını tercih edin:
  - Migration ekleme: `dotnet ef migrations add Initial --project src/Orbit.Infrastructure --startup-project src/Orbit.Web`
  - Uygulama: `dotnet ef database update --project src/Orbit.Infrastructure --startup-project src/Orbit.Web`

**Repository Kullanım Örneği**
- Okuma:
  - `IReadRepository<User, Guid>` üzerinden: `await repo.ListAsync(ct);`
  - Specification ile: `await repo.ListAsync(new UsersByQuerySpec("ali"), ct);`
- Yazma:
  - `await writeRepo.AddAsync(user, ct);`
  - `await unitOfWork.SaveChangesAsync(ct);`

**Tasarım Notları**
- DDD sınırları: Domain katmanı sadece arayüzleri ve specification sözleşmesini içerir; EF Core detayları Infrastructure’da kalır.
- CQRS uyumu: Okuma ve yazma yetenekleri `IReadRepository` ve `IWriteRepository` ile ayrıştırılmıştır; performans için okuma-odaklı `AsNoTracking()` varsayılan kabul edilmiştir.
- Genişletilebilirlik: Specification ile sorgular yeniden kullanılabilir, test edilebilir ve Infrastructure’a sızdırılmadan ifade edilebilir.
