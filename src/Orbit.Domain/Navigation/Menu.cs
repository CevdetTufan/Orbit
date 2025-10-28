using Orbit.Domain.Common;
using Orbit.Domain.Authorization;

namespace Orbit.Domain.Navigation;

public sealed class Menu : Entity<Guid>, IAggregateRoot
{
	private readonly List<Menu> _children = new();

	public string Title { get; private set; } = null!;
	public string? Url { get; private set; }
	public string? Description { get; private set; }

	// Parent relationship (nullable)
	public Guid? ParentId { get; private set; }
	public Menu? Parent { get; private set; }

	// Expose mutable collection so EF can track additions/removals reliably
	public ICollection<Menu> Children => _children;

	// Permission link (each menu baðlý olduðu yetkiye sahip) - nullable
	public Guid? PermissionId { get; private set; }
	public Permission? Permission { get; private set; }

	// UI / ordering fields
	public int Order { get; private set; }
	public bool Visible { get; private set; }
	public string? Icon { get; private set; }

	private Menu() { }

	private Menu(Guid id, string title, Permission? permission, string? url = null, string? description = null, Guid? parentId = null, int order = 0, bool visible = true, string? icon = null)
		: base(id)
	{
		Title = ValidateTitle(title);
		Url = url?.Trim();
		Description = description?.Trim();
		Permission = permission;
		PermissionId = permission?.Id;
		ParentId = parentId;

		Order = ValidateOrder(order);
		Visible = visible;
		Icon = ValidateIcon(icon);
	}

	public static Menu Create(string title, Permission? permission = null, string? url = null, string? description = null, Menu? parent = null, int order = 0, bool visible = true, string? icon = null)
		=> new(Guid.NewGuid(), title, permission, url, description, parent?.Id, order, visible, icon);

	public void Rename(string title) => Title = ValidateTitle(title);
	public void UpdateUrl(string? url) => Url = url?.Trim();
	public void UpdateDescription(string? description) => Description = description?.Trim();

	/// <summary>
	/// Set or remove parent. Also updates ParentId for EF.
	/// Avoids setting the menu as its own parent.
	/// </summary>
	public void SetParent(Menu? parent)
	{
		if (parent is not null && parent.Id == Id)
			throw new ArgumentException("A menu cannot be its own parent.", nameof(parent));

		Parent = parent;
		ParentId = parent?.Id;
	}

	public void SetPermission(Permission? permission)
	{
		Permission = permission;
		PermissionId = permission?.Id;
	}

	/// <summary>
	/// Set display order. Non-negative values only.
	/// </summary>
	public void SetOrder(int order) => Order = ValidateOrder(order);

	/// <summary>
	/// Show or hide the menu.
	/// </summary>
	public void SetVisible(bool visible) => Visible = visible;

	/// <summary>
	/// Update icon (font/icon class or path). Trimmed and validated.
	/// </summary>
	public void UpdateIcon(string? icon) => Icon = ValidateIcon(icon);

	/// <summary>
	/// Add a child menu and ensure child's parent is set to this.
	/// </summary>
	public void AddChild(Menu child)
	{
		if (child is null) throw new ArgumentNullException(nameof(child));
		if (child.Id == Id) throw new ArgumentException("A menu cannot be a child of itself.", nameof(child));
		if (_children.Any(x => x.Id == child.Id)) return;

		child.SetParent(this);
		_children.Add(child);
	}

	/// <summary>
	/// Remove a child menu and clear its parent.
	/// </summary>
	public void RemoveChild(Guid childId)
	{
		var child = _children.FirstOrDefault(x => x.Id == childId);
		if (child is null) return;
		child.SetParent(null);
		_children.Remove(child);
	}

	private static string ValidateTitle(string title)
	{
		if (string.IsNullOrWhiteSpace(title))
			throw new ArgumentException("Menu title is required", nameof(title));
		var trimmed = title.Trim();
		if (trimmed.Length is < 1 or > 200)
			throw new ArgumentException("Menu title length must be 1-200 characters", nameof(title));
		return trimmed;
	}

	private static int ValidateOrder(int order)
	{
		if (order < 0) throw new ArgumentOutOfRangeException(nameof(order), "Order must be non-negative.");
		return order;
	}

	private static string? ValidateIcon(string? icon)
	{
		if (icon is null) return null;
		var trimmed = icon.Trim();
		if (trimmed.Length > 100) throw new ArgumentException("Icon length must be 0-100 characters", nameof(icon));
		return trimmed.Length == 0 ? null : trimmed;
	}
}