\# Orbit – DDD \& CQRS Architectural Review Report  

\*(English \& Turkish Bilingual Version)\*



---



\## 🇬🇧 English Version



\### 1. Overview



\*\*Orbit\*\* is built using \*\*Domain-Driven Design (DDD)\*\* and \*\*CQRS (Command Query Responsibility Segregation)\*\* principles.  

Overall, the architecture is clean and layered (`Domain`, `Application`, `Infrastructure`, `Web`), but several DDD/CQRS rule violations and structural risks have been detected during code review.



---



\### 2. General Architecture Evaluation



✅ \*\*Strengths\*\*

\- Clear layer separation and modular structure.

\- Aggregate Roots, Entities, and Value Objects are correctly defined.

\- Domain events and repositories are implemented conceptually.



⚠️ \*\*Weak Points\*\*

\- Some \*\*domain logic leaks\*\* into Application layer (violating aggregate invariants).

\- \*\*Synchronous domain event dispatch\*\* risks data inconsistency.

\- \*\*Command and Query\*\* logic are mixed within the same services.

\- \*\*CancellationToken\*\* propagation is missing in repositories.

\- Blazor session management relies on in-memory state (no distributed support).



---



\### 3. DDD Assessment



| Aspect | Evaluation | Recommendation |

|--------|-------------|----------------|

| \*\*Aggregates \& Entities\*\* | Defined correctly, but some business rules live outside. | Move domain rules inside aggregate methods (`User.ChangeEmail()`, etc.). |

| \*\*Repositories\*\* | Abstractions exist but EF logic leaks upward. | Keep repository interfaces in Domain/Application, EF implementation in Infrastructure. |

| \*\*Domain Events\*\* | Used, but dispatched synchronously. | Implement \*\*Outbox pattern\*\* or async background publishing. |

| \*\*Boundaries\*\* | Domain project references are clean, but verify no EFCore or Infra dependencies. | Run `dotnet list package` on Domain to confirm. |



---



\### 4. CQRS Implementation Review



\- Commands and Queries are conceptually separated but physically mixed.

\- Handlers often include both read and write logic.

\- DTOs are not consistently used for read operations — domain entities are returned directly.



\*\*Recommendation:\*\*

\- Enforce strict separation between Command and Query handlers.  

\- Introduce projection models or DTOs for queries.

\- Use MediatR pipeline behaviors for validation, logging, and transactions.



---



\### 5. Detected Violations



\#### 🔴 Critical

1\. \*\*Synchronous Domain Event Dispatch\*\*

&nbsp;  - Risk: transaction inconsistency if event handler fails.

&nbsp;  - Fix: implement \*\*Outbox pattern\*\* or background publisher.



2\. \*\*Missing CancellationToken\*\*

&nbsp;  - All EF Core calls (`ToListAsync`, `SaveChangesAsync`, etc.) should accept and forward `CancellationToken`.



3\. \*\*In-Memory Session Management\*\*

&nbsp;  - `BlazorUserSessionManager` uses in-memory store; not scalable for multi-instance deployments.

&nbsp;  - Fix: use distributed cache (Redis) or SignalR backplane.



\#### 🟠 Important

4\. \*\*Domain–Infrastructure Leakage\*\*

&nbsp;  - Repository interfaces or EF details appear near Domain layer.

&nbsp;  - Fix: keep only abstractions in Domain/Application.



5\. \*\*Command/Query Mixing\*\*

&nbsp;  - Some services handle both responsibilities.

&nbsp;  - Fix: split them into separate handlers.



\#### 🟢 Improvements

\- Optimize domain event handler resolution (avoid reflection).

\- Introduce DTO mapping between Domain → API.



---



\### 6. Recommended Pull Requests (PR Plan)



| PR ID | Title | Priority | Summary |

|-------|--------|-----------|----------|

| \*\*PR-001\*\* | `add-cancellationtoken-to-repositories-and-uow` | 🔴 High | Add cancellation token support to async EF methods. |

| \*\*PR-002\*\* | `domain-events-outbox-pattern` | 🔴 High | Implement outbox table + background processor for event publishing. |

| \*\*PR-003\*\* | `blazor-session-redis-adapter` | 🔴 High | Replace in-memory Blazor session manager with Redis adapter. |

| \*\*PR-004\*\* | `separate-commands-and-queries` | 🟠 Medium | Refactor application layer into clear CQRS structure. |

| \*\*PR-005\*\* | `domain-infra-boundary-check` | 🟠 Medium | Remove EF references from Domain layer. |



---



\### 7. Testing Recommendations

\- \*\*Unit tests:\*\* Repository methods with `CancellationToken`.

\- \*\*Integration tests:\*\* Outbox message creation and processing.

\- \*\*End-to-end:\*\* Blazor session validation with Redis across multiple instances.



---



\### 8. Summary



The project demonstrates strong architectural intent with DDD and CQRS, but several structural improvements are required for \*\*transactional safety\*\*, \*\*scalability\*\*, and \*\*clear separation of concerns\*\*.  

Addressing the first three PRs (CancellationToken, Outbox, Redis) will significantly stabilize the system.



---



---



\## 🇹🇷 Türkçe Versiyon



\### 1. Genel Bakış



\*\*Orbit\*\*, \*\*Domain-Driven Design (DDD)\*\* ve \*\*CQRS (Command Query Responsibility Segregation)\*\* ilkeleriyle geliştirilmiştir.  

Genel mimari temiz ve katmanlı olsa da, kod incelemesi sonucunda bazı DDD/CQRS ihlalleri ve yapısal riskler tespit edilmiştir.



---



\### 2. Genel Mimari Değerlendirme



✅ \*\*Güçlü Yönler\*\*

\- Katmanlı yapı açık: `Domain`, `Application`, `Infrastructure`, `Web`.

\- Aggregate, Entity ve ValueObject kavramları doğru tanımlanmış.

\- Domain event ve repository yapıları konsept olarak mevcut.



⚠️ \*\*Zayıf Noktalar\*\*

\- Bazı iş kuralları Application katmanında — aggregate invariant ihlali riski.

\- Domain event’ler senkron olarak tetikleniyor (transaction hatası riski).

\- Command ve Query mantıkları karışmış.

\- Repositories’de `CancellationToken` eksik.

\- Blazor oturum yönetimi in-memory (çoklu sunucuya uygun değil).



---



\### 3. DDD Değerlendirmesi



| Kriter | Durum | Öneri |

|--------|--------|--------|

| \*\*Aggregate \& Entity\*\* | Doğru tanımlanmış, ancak bazı iş kuralları dışarıda. | Kuralları aggregate metotlarına taşı (`User.ChangeEmail()` vb.). |

| \*\*Repository\*\* | Abstraksiyonlar var ama EF sızıntısı olabiliyor. | Arayüzleri Domain/Application’da tut, implementasyon Infrastructure’da olsun. |

| \*\*Domain Events\*\* | Kullanılıyor ancak senkron. | \*\*Outbox pattern\*\* veya asenkron yayınlama ekle. |

| \*\*Sınırlar\*\* | Temiz ama EF referanslarını kontrol et. | `dotnet list package` ile doğrula. |



---



\### 4. CQRS Uygulaması



\- Command/Query yapısı karışmış.

\- Handler’larda hem okuma hem yazma işlemleri var.

\- Okuma işlemlerinde doğrudan entity döndürülüyor (DTO yok).



\*\*Öneriler:\*\*

\- Komut ve sorgu handler’larını tamamen ayır.

\- Okuma tarafı için projection/DTO modelleri oluştur.

\- MediatR pipeline’larını aktif kullan (validation, logging, transaction).



---



\### 5. Tespit Edilen İhlaller



\#### 🔴 Kritik

1\. \*\*Senkron Domain Event Dispatch\*\*  

&nbsp;  → Outbox pattern veya background publish kullanılmalı.  



2\. \*\*CancellationToken Eksikliği\*\*  

&nbsp;  → EF çağrılarına token parametresi eklenmeli.



3\. \*\*In-Memory Blazor Session\*\*  

&nbsp;  → Çoklu instance desteklemez. Redis veya SignalR backplane eklenmeli.



\#### 🟠 Önemli

4\. \*\*Domain–Infrastructure Sızıntısı\*\*  

&nbsp;  → Repository interface ve EF detayları domain’e yakın.  



5\. \*\*Command/Query Karışımı\*\*  

&nbsp;  → Ayrı handler’lara bölünmeli.



\#### 🟢 İyileştirme

\- Event handler çağrılarında reflection yerine DI kullanılmalı.

\- DTO katmanı eklenmeli.



---



\### 6. Önerilen PR Planı



| PR ID | Başlık | Öncelik | Açıklama |

|-------|---------|----------|-----------|

| \*\*PR-001\*\* | `add-cancellationtoken-to-repositories-and-uow` | 🔴 Yüksek | EF async metodlarına `CancellationToken` desteği ekle. |

| \*\*PR-002\*\* | `domain-events-outbox-pattern` | 🔴 Yüksek | Outbox tablosu + background event publisher uygula. |

| \*\*PR-003\*\* | `blazor-session-redis-adapter` | 🔴 Yüksek | Blazor session yönetimi için Redis adaptörü ekle. |

| \*\*PR-004\*\* | `separate-commands-and-queries` | 🟠 Orta | Command/Query handler’larını net ayır. |

| \*\*PR-005\*\* | `domain-infra-boundary-check` | 🟠 Orta | Domain katmanından EF referanslarını kaldır. |



---



\### 7. Test Önerileri

\- \*\*Unit test:\*\* `CancellationToken` içeren repository metodları.

\- \*\*Integration test:\*\* Outbox mesajının oluşturulup işlendiğini doğrula.

\- \*\*E2E test:\*\* Redis destekli Blazor session senaryosu.



---



\### 8. Özet



Proje güçlü bir DDD \& CQRS temeline sahip.  

Ancak, \*\*transaction güvenliği\*\*, \*\*ölçeklenebilirlik\*\* ve \*\*katman sınırlarının netliği\*\* için bazı mimari düzenlemeler gerekiyor.  

İlk üç PR’ın (CancellationToken, Outbox, Redis) uygulanması sistemin kararlılığını ciddi şekilde artıracaktır.



---



\*Prepared by: Architectural Review – Orbit Project (2025)\*



