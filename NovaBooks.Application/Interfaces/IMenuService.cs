using NovaBooks.Application.DTOs.Menu;

namespace NovaBooks.Application.Interfaces;

public interface IMenuService
{
    IReadOnlyCollection<MenuGroupResponse> GetMenu(
        IEnumerable<string> permissions);
}
