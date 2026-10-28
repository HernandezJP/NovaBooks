using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Devoluciones de clientes. Registrar requiere Ventas.Devolver; anular una
/// devolución revierte inventario, saldo y reembolso, y requiere Ventas.Anular.
/// </summary>
[AuditEntity("DevolucionVenta")]
[ApiController]
[Route("api/devoluciones")]
public sealed class ReturnsController : ControllerBase
{
    private readonly IReturnService _returns;

    public ReturnsController(IReturnService returns)
    {
        _returns = returns;
    }

    [HasAnyPermission(SalesPermissions.Return, SalesPermissions.View)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(ReturnCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReturnCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken) =>
        Ok(await _returns.GetCatalogsAsync(cancellationToken));

    [HasAnyPermission(SalesPermissions.Return, SalesPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ReturnListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ReturnListItemResponse>>> GetPaged(
        [FromQuery] ReturnQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _returns.GetPagedAsync(query, cancellationToken));

    [HasAnyPermission(SalesPermissions.Return, SalesPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        ReturnResponse? entry = await _returns.GetByIdAsync(id, cancellationToken);

        return entry is null ? this.NotFoundProblem("La devolución no existe.") : Ok(entry);
    }

    [HasPermission(SalesPermissions.Return)]
    [HttpPost]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnResponse>> Create(
        [FromBody] SaveReturnRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<ReturnResponse> result =
            await _returns.CreateAsync(request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(SalesPermissions.Cancel)]
    [HttpPost("{id:int}/anular")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnResponse>> Annul(
        int id,
        [FromBody] AnnulReturnRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<ReturnResponse> result =
            await _returns.AnnulAsync(id, request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
