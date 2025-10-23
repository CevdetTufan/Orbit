namespace Orbit.Application.Navigation;

public interface IMenuCommands
{
    /// <summary>
    /// Creates a menu and returns created id.
    /// </summary>
    Task<Guid> CreateAsync(
        string title,
        Guid permissionId,
        string? url = null,
        string? description = null,
        Guid? parentId = null,
        int order = 0,
        bool visible = true,
        string? icon = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string title,
        Guid permissionId,
        string? url = null,
        string? description = null,
        Guid? parentId = null,
        int order = 0,
        bool visible = true,
        string? icon = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetVisibilityAsync(Guid id, bool visible, CancellationToken cancellationToken = default);
    Task SetOrderAsync(Guid id, int order, CancellationToken cancellationToken = default);
    Task SetParentAsync(Guid id, Guid? parentId, CancellationToken cancellationToken = default);
}
