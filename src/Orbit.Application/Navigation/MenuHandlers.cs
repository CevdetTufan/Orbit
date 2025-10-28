using Orbit.Application.Navigation.Models;

namespace Orbit.Application.Navigation;

internal sealed class MenuCommandHandler : IMenuCommandHandler
{
    private readonly IMenuCommands _commands;

    public MenuCommandHandler(IMenuCommands commands) => _commands = commands;

    public Task<Guid> HandleCreateAsync(CreateMenuCommand command, CancellationToken cancellationToken = default)
        => _commands.CreateAsync(
            command.Title,
            command.PermissionId,
            command.Url,
            command.Description,
            command.ParentId,
            command.Order,
            command.Visible,
            command.Icon,
            cancellationToken);

    public Task HandleUpdateAsync(UpdateMenuCommand command, CancellationToken cancellationToken = default)
        => _commands.UpdateAsync(
            command.Id,
            command.Title,
            command.PermissionId,
            command.Url,
            command.Description,
            command.ParentId,
            command.Order,
            command.Visible,
            command.Icon,
            cancellationToken);

    public Task HandleDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _commands.DeleteAsync(id, cancellationToken);
}

internal sealed class MenuQueryHandler : IMenuQueryHandler
{
    private readonly IMenuQueries _queries;

    public MenuQueryHandler(IMenuQueries queries) => _queries = queries;

    public Task<IReadOnlyList<MenuDto>> HandleGetAllAsync(CancellationToken cancellationToken = default)
        => _queries.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<MenuDto>> HandleGetTreeAsync(CancellationToken cancellationToken = default)
        => _queries.GetTreeAsync(cancellationToken);

    public Task<MenuDetailDto?> HandleGetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _queries.GetByIdAsync(id, cancellationToken);
}
