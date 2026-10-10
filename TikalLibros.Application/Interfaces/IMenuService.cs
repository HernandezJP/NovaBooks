using TikalLibros.Application.DTOs.Menu;

namespace TikalLibros.Application.Interfaces;

public interface IMenuService
{
    IReadOnlyCollection<MenuGroupResponse> GetMenu(
        IEnumerable<string> permissions);
}
