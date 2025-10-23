using Orbit.Application.Navigation.Models;
using Orbit.Domain.Navigation;
using Orbit.Domain.Common;

namespace Orbit.Application.Navigation;

public interface IMenuQueries
{
    Task<IReadOnlyList<MenuDto>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns menu tree (root nodes with nested children).
    /// Useful for building navigation in the UI.
    /// </summary>
    Task<IReadOnlyList<MenuDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<MenuDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class MenuQueries : IMenuQueries
{
    private readonly IRepository<Menu, Guid> _readRepository;

    public MenuQueries(IRepository<Menu, Guid> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IReadOnlyList<MenuDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var menus = await _readRepository.ListAsync(cancellationToken: cancellationToken);
        return menus.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<MenuDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var menus = (await _readRepository.ListAsync(cancellationToken: cancellationToken))
                    .Select(MapToDto).ToList();

        // build tree in-memory
        var lookup = menus.ToDictionary(m => m.Id);
        var roots = new List<MenuDto>();

        foreach (var menu in menus)
        {
            if (menu.ParentId is null)
            {
                roots.Add(menu);
            }
            else if (lookup.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children.Add(menu);
            }
            else
            {
                // Orphaned node - treat as root
                roots.Add(menu);
            }
        }

        // sort children by Order for predictable UI
        void SortRecursively(MenuDto m)
        {
            m.Children.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var c in m.Children) SortRecursively(c);
        }

        foreach (var r in roots) SortRecursively(r);

        return roots;
    }

    public async Task<MenuDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // simplest approach: load all and find with children built (avoids needing specs)
        var menus = (await _readRepository.ListAsync(cancellationToken: cancellationToken))
                    .Select(MapToDto).ToList();

        var lookup = menus.ToDictionary(m => m.Id);
        foreach (var menu in menus)
        {
            if (menu.ParentId is not null && lookup.TryGetValue(menu.ParentId.Value, out var p))
            {
                p.Children.Add(menu);
            }
        }

        if (!lookup.TryGetValue(id, out var found)) return null;

        // build MenuDetailDto including nested children
        MenuDetailDto BuildDetail(MenuDto dto)
            => new(dto.Id, dto.Title, dto.PermissionId, dto.Url, dto.Description, dto.ParentId, dto.Order, dto.Visible, dto.Icon)
            {
                Children = dto.Children.Select(BuildDetail).ToList().AsReadOnly()
            };

        return BuildDetail(found);
    }

    private static MenuDto MapToDto(Menu m)
        => new MenuDto(
            m.Id,
            m.Title,
            m.PermissionId,
            m.Url,
            m.Description,
            m.ParentId,
            m.Order,
            m.Visible,
            m.Icon);
}
