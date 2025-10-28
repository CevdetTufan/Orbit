namespace Orbit.Application.Navigation.Models;

public sealed class MenuDto
{
    public MenuDto(Guid id, string title, Guid? permissionId, string? url = null, string? description = null, Guid? parentId = null, int order = 0, bool visible = true, string? icon = null)
    {
        Id = id;
        Title = title;
        PermissionId = permissionId;
        Url = url;
        Description = description;
        ParentId = parentId;
        Order = order;
        Visible = visible;
        Icon = icon;
        Children = new List<MenuDto>();
    }

    public Guid Id { get; }
    public string Title { get; }
    public string? Url { get; }
    public string? Description { get; }
    public Guid? PermissionId { get; }
    public Guid? ParentId { get; }
    public int Order { get; }
    public bool Visible { get; }
    public string? Icon { get; }

    // mutable list makes building tree simpler; DTO is used for transfer to UI
    public List<MenuDto> Children { get; }
}
