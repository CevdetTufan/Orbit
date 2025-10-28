using Orbit.Application.Navigation.Models;

namespace Orbit.Application.Navigation;

// Command and Query handler interfaces for DI and unit testing convenience
public interface IMenuCommandHandler
{
    Task<Guid> HandleCreateAsync(CreateMenuCommand command, CancellationToken cancellationToken = default);
    Task HandleUpdateAsync(UpdateMenuCommand command, CancellationToken cancellationToken = default);
    Task HandleDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IMenuQueryHandler
{
    Task<IReadOnlyList<MenuDto>> HandleGetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuDto>> HandleGetTreeAsync(CancellationToken cancellationToken = default);
    Task<MenuDetailDto?> HandleGetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

// Small request DTOs for handlers
public sealed class CreateMenuCommand
{
    public string Title { get; init; } = null!;
    public Guid? PermissionId { get; init; }
    public string? Url { get; init; }
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public int Order { get; init; }
    public bool Visible { get; init; } = true;
    public string? Icon { get; init; }
}

public sealed class UpdateMenuCommand
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public Guid? PermissionId { get; init; }
    public string? Url { get; init; }
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public int Order { get; init; }
    public bool Visible { get; init; } = true;
    public string? Icon { get; init; }
}
