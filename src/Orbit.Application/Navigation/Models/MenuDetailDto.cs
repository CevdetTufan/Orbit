namespace Orbit.Application.Navigation.Models;

public sealed class MenuDetailDto
{
    public MenuDetailDto(Guid id, string title, Guid? permissionId, string? url = null, string? description = null, Guid? parentId = null, int order = 0, bool visible = true, string? icon = null)
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
        Children = new List<MenuDetailDto>().AsReadOnly();
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

    public IReadOnlyList<MenuDetailDto> Children { get; init; }
}
