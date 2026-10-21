using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Proveedores de libros. No se desactivan con órdenes de compra abiertas.
/// </summary>
[AuditEntity("Proveedor")]
[ApiController]
[Route("api/proveedores")]
public sealed class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HasPermission(SupplierPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SupplierListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SupplierListItemResponse>>> GetPaged(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? personType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _supplierService.GetPagedAsync(search, isActive, personType, page, pageSize, cancellationToken));
    }

    [HasPermission(SupplierPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        SupplierResponse? supplier = await _supplierService.GetByIdAsync(id, cancellationToken);

        return supplier is null
            ? this.NotFoundProblem("El proveedor solicitado no existe.")
            : Ok(supplier);
    }

    [HasAnyPermission(SupplierPermissions.View, SupplierPermissions.Create, SupplierPermissions.Update)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(SupplierCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetCatalogsAsync(cancellationToken));
    }

    [HasPermission(SupplierPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierResponse>> Create(
        [FromBody] SaveSupplierRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SupplierResponse> result = await _supplierService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(SupplierPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierResponse>> Update(
        int id,
        [FromBody] SaveSupplierRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SupplierResponse> result = await _supplierService.UpdateAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(SupplierPermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeSupplierStatusRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SupplierResponse> result = await _supplierService.ChangeStatusAsync(
            id, request.IsActive, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
