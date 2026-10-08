using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaBooks.Application.DTOs.Menu;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

[ApiController]
[Route("api/menu")]
[Authorize]
public sealed class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// Menú filtrado con los permisos vigentes del usuario.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<MenuGroupResponse>),
        StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<MenuGroupResponse>> Get()
    {
        IEnumerable<string> permissions = User
            .FindAll(CustomClaimTypes.Permission)
            .Select(claim => claim.Value);

        return Ok(_menuService.GetMenu(permissions));
    }
}
