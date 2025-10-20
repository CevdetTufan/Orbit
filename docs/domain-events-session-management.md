# Domain Events ve Oturum Yönetimi

## 📋 Genel Bakış

Bu dokümantasyon, Orbit projesinde kullanıcı pasifleştirme işlemi sonrası otomatik oturum sonlandırma mekanizmasının teknik detaylarını açıklar.

## 🎯 İş Gereksinimi

**Senaryo:** Bir kullanıcı admin tarafından pasifleştirildiğinde:
1. **Aktif oturum varsa** → Kullanıcı anında siteden atılmalı
2. **Oturum kapalıysa** → Bir sonraki giriş denemesinde engellenmelidir

## 🏗️ Mimari Tasarım

### Kullanılan Yaklaşım: Domain Events + Event Handlers

```
┌─────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  User.Deactivate()                                       │   │
│  │    • IsActive = false                                    │   │
│  │    • AddDomainEvent(UserDeactivatedEvent)                │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  UserCommands.DeactivateAsync()                          │   │
│  │    1. user.Deactivate()                                  │   │
│  │    2. _unitOfWork.SaveChanges()                          │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                     INFRASTRUCTURE LAYER                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  UnitOfWork.SaveChangesAsync()                           │   │
│  │    1. Collect domain events from entities                │   │
│  │    2. Save to database ✅                                │   │
│  │    3. Dispatch events                                    │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  UserDeactivatedEventHandler.HandleAsync()               │   │
│  │    • _sessionManager.TerminateUserSessions()             │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                         WEB LAYER                               │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  BlazorUserSessionManager                                │   │
│  │    • Find all circuits for username                      │   │
│  │    • CircuitTerminationNotifier.NotifyTermination()      │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  CircuitAuthenticationStateProvider                      │   │
│  │    • ForceLogoutAsync()                                  │   │
│  │    • Redirect to /login?reason=deactivated               │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📦 Bileşenler

### 1. Domain Layer

#### `IDomainEvent` Interface
```csharp
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
```
- Marker interface
- Tüm domain event'ler bunu implement eder

#### `Entity<TId>` Base Class
```csharp
public abstract class Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```
- Her aggregate root event saklayabilir
- Event'ler SaveChanges'e kadar bellekte tutulur

#### `UserDeactivatedEvent`
```csharp
public sealed record UserDeactivatedEvent(Guid UserId, string Username) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```
- Immutable (record)
- Kullanıcı bilgilerini taşır

#### `User` Aggregate
```csharp
public void Deactivate()
{
    if (!IsActive) return;
    
    IsActive = false;
    AddDomainEvent(new UserDeactivatedEvent(Id, Username.Value));
}
```
- İş kuralını uygular
- Event fırlatır

---

### 2. Application Layer

#### `IDomainEventDispatcher` Interface
```csharp
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
```

#### `DomainEventDispatcher` Implementation
```csharp
public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
{
    var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
    var handlers = _serviceProvider.GetServices(handlerType);
    
    foreach (var handler in handlers)
    {
        if (handler == null) continue;
        
        var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
        if (method != null)
        {
            var result = method.Invoke(handler, new object[] { domainEvent, cancellationToken });
            if (result is Task task)
            {
                await task;
            }
        }
    }
}
```
- Generic handler resolution
- Multiple handler desteği
- Reflection ile dynamic invocation

#### `IDomainEventHandler<T>` Interface
```csharp
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
```

#### `UserDeactivatedEventHandler`
```csharp
public async Task HandleAsync(UserDeactivatedEvent domainEvent, CancellationToken cancellationToken)
{
    _logger.LogInformation("User deactivated: {Username}", domainEvent.Username);
    
    try
    {
        await _sessionManager.TerminateUserSessionsAsync(domainEvent.Username, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to terminate sessions for {Username}", domainEvent.Username);
        // Session termination hatası ana işlemi bozmamalı
    }
}
```
- Event'e tepki verir
- Session manager'ı çağırır
- Hata yönetimi defensive

#### `IUserSessionManager` Interface
```csharp
public interface IUserSessionManager
{
    Task TerminateUserSessionsAsync(string username, CancellationToken cancellationToken = default);
}
```

---

### 3. Infrastructure Layer

#### `UnitOfWork` Event Dispatching
```csharp
public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // 1. Entity'lerden event'leri topla
    var domainEntities = _dbContext.ChangeTracker
        .Entries<Entity<Guid>>()
        .Where(x => x.Entity.DomainEvents.Any())
        .Select(x => x.Entity)
        .ToList();

    var domainEvents = domainEntities
        .SelectMany(x => x.DomainEvents)
        .ToList();

    // 2. Önce database'e kaydet
    var result = await _dbContext.SaveChangesAsync(cancellationToken);

    // 3. Sonra event'leri dispatch et
    foreach (var domainEvent in domainEvents)
    {
        await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
    }

    // 4. Event'leri temizle
    domainEntities.ForEach(entity => entity.ClearDomainEvents());

    return result;
}
```

**⚠️ Kritik:** Event'ler SaveChanges **başarılı** olduktan sonra dispatch edilir!

---

### 4. Web Layer (Blazor Server)

#### `BlazorUserSessionManager`
```csharp
public sealed class BlazorUserSessionManager : IUserSessionManager
{
    // Username → Circuit ID mapping
    private readonly ConcurrentDictionary<string, HashSet<string>> _userCircuits = new();
    
    public void RegisterCircuit(string username, string circuitId)
    {
        _userCircuits.AddOrUpdate(
            username,
            _ => new HashSet<string> { circuitId },
            (_, circuits) =>
            {
                lock (circuits) { circuits.Add(circuitId); }
                return circuits;
            });
    }
    
    public Task TerminateUserSessionsAsync(string username, CancellationToken cancellationToken)
    {
        if (_userCircuits.TryRemove(username, out var circuits))
        {
            foreach (var circuitId in circuits)
            {
                CircuitTerminationNotifier.NotifyTermination(circuitId);
            }
        }
        return Task.CompletedTask;
    }
}
```
- **Singleton** lifetime
- Thread-safe dictionary
- Circuit tracking

#### `CircuitTerminationNotifier` (Static Helper)
```csharp
public static class CircuitTerminationNotifier
{
    private static readonly ConcurrentDictionary<string, Action> _terminationCallbacks = new();
    
    public static void RegisterCallback(string circuitId, Action callback)
    {
        _terminationCallbacks[circuitId] = callback;
    }
    
    public static void NotifyTermination(string circuitId)
    {
        if (_terminationCallbacks.TryGetValue(circuitId, out var callback))
        {
            callback?.Invoke();
        }
    }
}
```
- Callback pattern
- Circuit → Action mapping

#### `CircuitAuthenticationStateProvider`
```csharp
public async Task SignInAsync(AuthTokenDto token)
{
    // ... auth logic ...
    
    if (_currentCircuitId != null)
    {
        _sessionManager.RegisterCircuit(token.Username, _currentCircuitId);
        
        CircuitTerminationNotifier.RegisterCallback(_currentCircuitId, () =>
        {
            _ = ForceLogoutAsync();
        });
    }
}

private async Task ForceLogoutAsync()
{
    await SignOutAsync();
    _navigationManager.NavigateTo("/login?reason=deactivated", forceLoad: true);
}
```

---

## 🔄 Akış Diyagramı

### Normal Kullanıcı Giriş/Çıkış Akışı
```
User Login
    ↓
SignInAsync()
    ↓
SessionManager.RegisterCircuit(username, circuitId)
    ↓
CircuitTerminationNotifier.RegisterCallback(circuitId, ForceLogoutAsync)
    ↓
User Active Session ✅

User Logout
    ↓
SignOutAsync()
    ↓
SessionManager.UnregisterCircuit(username, circuitId)
    ↓
CircuitTerminationNotifier.UnregisterCallback(circuitId)
    ↓
Session Terminated ✅
```

### Kullanıcı Pasifleştirme Akışı
```
Admin → UserCreation Page
    ↓
Click "Pasifleştir"
    ↓
UserCommands.DeactivateAsync(userId)
    ↓
user.Deactivate()
    • IsActive = false
    • AddDomainEvent(UserDeactivatedEvent)
    ↓
_unitOfWork.SaveChangesAsync()
    ↓
UnitOfWork:
    1. Collect events from entities
    2. SaveChanges to DB ✅ (Transaction committed)
    3. Dispatch events
    ↓
DomainEventDispatcher.DispatchAsync(UserDeactivatedEvent)
    ↓
UserDeactivatedEventHandler.HandleAsync()
    ↓
SessionManager.TerminateUserSessionsAsync(username)
    • Find all circuits for username
    • Remove from _userCircuits dictionary
    ↓
For each circuit:
    CircuitTerminationNotifier.NotifyTermination(circuitId)
    ↓
    Callback invoked → ForceLogoutAsync()
    ↓
    SignOutAsync()
    • Clear localStorage
    • Update ClaimsPrincipal
    ↓
    NavigateTo("/login?reason=deactivated", forceLoad: true)
    ↓
User forcefully logged out ✅
```

---

## 🧪 Test Senaryoları

### Senaryo 1: Tek Oturum - Aktif Kullanıcı
```
1. Browser A: User olarak login
2. Browser B: Admin olarak login
3. Admin: User'ı pasifleştir
4. ✅ Browser A: Anında logout → /login?reason=deactivated
```

### Senaryo 2: Çoklu Oturum - Aynı Kullanıcı
```
1. Browser A: User olarak login
2. Browser B: Aynı user ile login
3. Admin: User'ı pasifleştir
4. ✅ Browser A: Logout
5. ✅ Browser B: Logout
```

### Senaryo 3: Oturum Kapalı - Pasif Kullanıcı Giriş Denemesi
```
1. Admin: User'ı pasifleştir
2. Login sayfası: Pasif user credentials ile giriş dene
3. ✅ AuthService.AuthenticateAsync() → IsActive kontrolü
4. ✅ Hata: "Hesabınız pasif"
```

### Senaryo 4: Event Handler Hatası
```
1. SessionManager exception fırlatsın
2. Admin: User'ı pasifleştir
3. ✅ DB'de user pasif (SaveChanges başarılı)
4. ⚠️ Log: Session termination failed
5. ✅ Kullanıcı next request'te IsActive kontrolünde yakalanır
```

---

## 🔐 Güvenlik ve İstisna Durumlar

### Transaction Safety
```csharp
// ❌ YANLIŞ SIRA
user.Deactivate();
await TerminateSessionsAsync(); // 🚫 DB henüz commit olmadı!
await SaveChangesAsync();       // Hata olursa session terminate olmuş kalır

// ✅ DOĞRU SIRA
user.Deactivate();
await SaveChangesAsync();       // ✅ Önce DB commit
await TerminateSessionsAsync(); // ✅ Sonra session terminate
```

### Event Handler Exception Handling
```csharp
try
{
    await _sessionManager.TerminateUserSessionsAsync(username);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Session termination failed");
    // ✅ Exception suppress edilir, ana işlem başarılı
    // ✅ Kullanıcı zaten DB'de pasif, next request'te yakalanır
}
```

### Concurrency Handling
```csharp
// UserCommands.DeactivateAsync içinde
try
{
    user.Deactivate();
    await _unitOfWork.SaveChangesAsync();
}
catch (Exception ex) when (IsConcurrencyException(ex))
{
    // Retry mechanism
    await HandleConcurrencyAndRetryAsync(async () =>
    {
        var freshUser = await _userRepository.GetByIdAsync(id);
        freshUser.Deactivate();
        await _unitOfWork.SaveChangesAsync();
    });
}
```

---

## 📊 Performans Considerations

### Singleton vs Scoped
```csharp
// ✅ Singleton - Circuit tracking tüm app'te paylaşımlı
services.AddSingleton<BlazorUserSessionManager>();

// ✅ Scoped - Her request için resolve
services.AddScoped<IUserSessionManager>(sp => 
    sp.GetRequiredService<BlazorUserSessionManager>());
```

### Memory Management
- `ConcurrentDictionary<string, HashSet<string>>` → Thread-safe
- Circuit disconnect olduğunda cleanup → `IDisposable`
- Memory leak riski: Circuit'ler unregister edilmezse
  - ✅ `Dispose()` pattern ile otomatik cleanup

### Scalability
**Single Server:**
- ✅ In-memory dictionary yeterli
- ✅ Circuit tracking overhead minimal

**Multi-Server (Load Balanced):**
- ❌ In-memory dictionary çalışmaz (farklı sunucular farklı memory)
- ✅ Gerekli: **Distributed Cache** (Redis)
- ✅ Gerekli: **SignalR Backplane**

---

## 🚀 Gelecek Geliştirmeler

### SignalR ile Real-Time Notification
```csharp
public class UserHub : Hub
{
    public async Task NotifyUserDeactivated(string username)
    {
        await Clients.User(username).SendAsync("ForceLogout");
    }
}

// Event Handler
public async Task HandleAsync(UserDeactivatedEvent evt, ...)
{
    await _hubContext.Clients.User(evt.Username).SendAsync("ForceLogout");
}

// Client (JavaScript)
hubConnection.on("ForceLogout", () => {
    window.location.href = "/login?reason=deactivated";
});
```

**Avantajları:**
- Çoklu cihaz desteği
- Tarayıcılar arası senkronizasyon
- Load balanced ortamlarda çalışır

### Distributed Cache (Redis)
```csharp
public class RedisUserSessionManager : IUserSessionManager
{
    private readonly IDistributedCache _cache;
    
    public async Task RegisterCircuitAsync(string username, string circuitId)
    {
        var key = $"user-circuits:{username}";
        // Redis Set operations
    }
}
```

---

## 📚 Referanslar

### Domain-Driven Design
- **Aggregate Root:** User entity
- **Domain Event:** UserDeactivatedEvent
- **Event Handler:** Application layer concern
- **Infrastructure:** Event dispatching mechanism

### CQRS Pattern
- **Command:** DeactivateUserCommand → UserCommands.DeactivateAsync()
- **Event:** UserDeactivatedEvent
- **Handler:** UserDeactivatedEventHandler

### Clean Architecture
```
Domain (Core)
    ↑
Application (Use Cases)
    ↑
Infrastructure (Persistence, External)
    ↑
Presentation (Web, API)
```

---

## 🔧 Sorun Giderme

### Problem: "Unable to resolve service for type 'IUserSessionManager'"
**Çözüm:** Program.cs'de DI kayıtları eksik
```csharp
builder.Services.AddSingleton<BlazorUserSessionManager>();
builder.Services.AddScoped<IUserSessionManager>(sp => 
    sp.GetRequiredService<BlazorUserSessionManager>());
```

### Problem: Kullanıcı logout olmuyor
**Kontrol:**
1. Circuit ID doğru mu? (`_httpContextAccessor.HttpContext?.Connection.Id`)
2. Callback register edilmiş mi? (`CircuitTerminationNotifier.RegisterCallback`)
3. SessionManager'a circuit kaydedilmiş mi? (`RegisterCircuit`)

### Problem: Event handler çalışmıyor
**Kontrol:**
1. Event handler DI'da kayıtlı mı?
   ```csharp
   services.AddScoped<IDomainEventHandler<UserDeactivatedEvent>, UserDeactivatedEventHandler>();
   ```
2. UnitOfWork event dispatching yapıyor mu?
3. Domain entity event ekliyor mu? (`AddDomainEvent`)

---

## ✅ Checklist

### Domain Layer
- [x] `IDomainEvent` interface
- [x] `Entity<TId>` base class (DomainEvents property)
- [x] `UserDeactivatedEvent` record
- [x] `User.Deactivate()` metodu event fırlatıyor

### Application Layer
- [x] `IDomainEventDispatcher` interface
- [x] `DomainEventDispatcher` implementation
- [x] `IDomainEventHandler<T>` interface
- [x] `UserDeactivatedEventHandler`
- [x] `IUserSessionManager` interface
- [x] DI kayıtları

### Infrastructure Layer
- [x] `UnitOfWork` event dispatching
- [x] SaveChanges → Dispatch → ClearEvents akışı

### Web Layer
- [x] `BlazorUserSessionManager` implementation
- [x] `CircuitTerminationNotifier` static helper
- [x] `CircuitAuthenticationStateProvider` circuit tracking
- [x] DI kayıtları (Singleton + Scoped)

---

## 📞 İletişim ve Destek

Sorularınız veya önerileriniz için:
- GitHub Issues
- Pull Requests
- Code Reviews

---
