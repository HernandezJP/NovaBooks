using Microsoft.AspNetCore.Mvc;
using NovaBooks.Api.Common;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Editorials;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

/// <summary>
/// Editoriales del catálogo. Usa los permisos de Catálogo.
/// </summary>
[ApiController]
[Route("api/editoriales")]
public sealed class EditorialsController : ControllerBase
{
    private readonly IEditorialService _editorialService;

    public EditorialsController(IEditorialService editorialService)
    {
        _editorialService = editorialService;
    }

    [HasPermission(CatalogPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EditorialResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EditorialResponse>>> GetPaged(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _editorialService.GetPagedAsync(search, isActive, page, pageSize, cancellationToken));
    }

    [HasPermission(CatalogPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EditorialResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        EditorialResponse? editorial = await _editorialService.GetByIdAsync(id, cancellationToken);

        return editorial is null
            ? this.NotFoundProblem("La editorial solicitada no existe.")
            : Ok(editorial);
    }

    [HasPermission(CatalogPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EditorialResponse>> Create(
        [FromBody] SaveEditorialRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<EditorialResponse> result = await _editorialService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(CatalogPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EditorialResponse>> Update(
        int id,
        [FromBody] SaveEditorialRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<EditorialResponse> result = await _editorialService.UpdateAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(CatalogPermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EditorialResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeEditorialStatusRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<EditorialResponse> result =
            await _editorialService.ChangeStatusAsync(id, request.IsActive, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
