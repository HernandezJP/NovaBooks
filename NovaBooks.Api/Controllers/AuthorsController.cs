using Microsoft.AspNetCore.Mvc;
using NovaBooks.Api.Common;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Authors;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

/// <summary>
/// Autores del catálogo. Usa los permisos de Catálogo.
/// </summary>
[ApiController]
[Route("api/autores")]
public sealed class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    [HasPermission(CatalogPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuthorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AuthorResponse>>> GetPaged(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _authorService.GetPagedAsync(search, isActive, page, pageSize, cancellationToken));
    }

    [HasPermission(CatalogPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AuthorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        AuthorResponse? author = await _authorService.GetByIdAsync(id, cancellationToken);

        return author is null
            ? this.NotFoundProblem("El autor solicitado no existe.")
            : Ok(author);
    }

    [HasPermission(CatalogPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(AuthorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorResponse>> Create(
        [FromBody] SaveAuthorRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<AuthorResponse> result = await _authorService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(CatalogPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AuthorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorResponse>> Update(
        int id,
        [FromBody] SaveAuthorRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<AuthorResponse> result = await _authorService.UpdateAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(CatalogPermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(AuthorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeAuthorStatusRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<AuthorResponse> result =
            await _authorService.ChangeStatusAsync(id, request.IsActive, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
