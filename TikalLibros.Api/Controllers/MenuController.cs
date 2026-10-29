using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TikalLibros.Application.DTOs.Menu;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

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
