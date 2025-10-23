using Orbit.Domain.Navigation;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Navigation;

internal sealed class MenuCommands : IMenuCommands
{
    private readonly IRepository<Menu, Guid> _menuRepository;
    private readonly IRepository<Permission, Guid> _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MenuCommands(
        IRepository<Menu, Guid> menuRepository,
        IRepository<Permission, Guid> permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(
        string title,
        Guid permissionId,
        string? url = null,
        string? description = null,
        Guid? parentId = null,
        int order = 0,
        bool visible = true,
        string? icon = null,
        CancellationToken cancellationToken = default)
    {
        var permission = await GetTrackedPermissionAsync(permissionId, cancellationToken);
        Menu? parent = null;
        if (parentId is not null)
        {
            parent = await GetTrackedMenuAsync(parentId.Value, cancellationToken);
        }

        var menu = Menu.Create(title, permission, url, description, parent, order, visible, icon);
        await _menuRepository.AddAsync(menu, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return menu.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string title,
        Guid permissionId,
        string? url = null,
        string? description = null,
        Guid? parentId = null,
        int order = 0,
        bool visible = true,
        string? icon = null,
        CancellationToken cancellationToken = default)
    {
        var menu = await GetTrackedMenuAsync(id, cancellationToken);

        // update title, url, description
        menu.Rename(title);
        menu.UpdateUrl(url);
        menu.UpdateDescription(description);

        // permission
        if (menu.PermissionId != permissionId)
        {
            var permission = await GetTrackedPermissionAsync(permissionId, cancellationToken);
            menu.SetPermission(permission);
        }

        // parent
        if (parentId is null)
        {
            menu.SetParent(null);
        }
        else
        {
            if (parentId == id) throw new ArgumentException("A menu cannot be its own parent.", nameof(parentId));
            var parent = await GetTrackedMenuAsync(parentId.Value, cancellationToken);
            menu.SetParent(parent);
        }

        menu.SetOrder(order);
        menu.SetVisible(visible);
        menu.UpdateIcon(icon);

        _menuRepository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var menu = await GetTrackedMenuAsync(id, cancellationToken);

        // prevent deleting a menu that has children
        if (menu.Children.Any())
            throw new InvalidOperationException("Cannot delete a menu that has child menus. Remove or reassign children first.");

        _menuRepository.Remove(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetVisibilityAsync(Guid id, bool visible, CancellationToken cancellationToken = default)
    {
        var menu = await GetTrackedMenuAsync(id, cancellationToken);
        menu.SetVisible(visible);
        _menuRepository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetOrderAsync(Guid id, int order, CancellationToken cancellationToken = default)
    {
        var menu = await GetTrackedMenuAsync(id, cancellationToken);
        menu.SetOrder(order);
        _menuRepository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetParentAsync(Guid id, Guid? parentId, CancellationToken cancellationToken = default)
    {
        var menu = await GetTrackedMenuAsync(id, cancellationToken);
        if (parentId is null)
        {
            menu.SetParent(null);
        }
        else
        {
            if (parentId == id) throw new ArgumentException("A menu cannot be its own parent.", nameof(parentId));
            var parent = await GetTrackedMenuAsync(parentId.Value, cancellationToken);
            menu.SetParent(parent);
        }

        _menuRepository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // helpers
    private async Task<Menu> GetTrackedMenuAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _menuRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Menu with ID {id} not found");
    }

    private async Task<Permission> GetTrackedPermissionAsync(Guid permissionId, CancellationToken cancellationToken)
    {
        return await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission with ID {permissionId} not found");
    }
}
