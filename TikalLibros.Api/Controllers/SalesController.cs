using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Ventas: facturación de pedidos reservados y ventas de mostrador, con
/// pagos, salida automática de inventario y cobro en la caja abierta del
/// usuario. Anular revierte inventario, pagos y movimientos de caja.
/// </summary>
[AuditEntity("Venta")]
[ApiController]
[Route("api/ventas")]
public sealed class SalesController : ControllerBase
{
    private readonly ISaleService _sales;

    public SalesController(ISaleService sales)
    {
        _sales = sales;
    }

    private SalesPermissionSet Permissions => new(
        this.HasPermission(SalesPermissions.ApplyDiscount),
        this.HasPermission(SalesPermissions.SellOnCredit),
        this.HasPermission(OrderPermissions.Approve));

    [HasAnyPermission(SalesPermissions.View, SalesPermissions.Create)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(SaleCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SaleCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _sales.GetCatalogsAsync(cancellationToken));
    }

    [HasPermission(SalesPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SaleListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SaleListItemResponse>>> GetPaged(
        [FromQuery] SaleQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sales.GetPagedAsync(query, cancellationToken));
    }

    [HasPermission(SalesPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        SaleResponse? sale = await _sales.GetByIdAsync(id, this.HasPermission(InventoryPermissions.ViewCosts), cancellationToken);

        return sale is null ? this.NotFoundProblem("La venta no existe.") : Ok(sale);
    }

    /// <summary>Factura electrónica de la venta (certificación simulada) para mostrarla o imprimirla.</summary>
    [HasPermission(SalesPermissions.View)]
    [HttpGet("{id:int}/factura")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(int id, CancellationToken cancellationToken)
    {
        InvoiceResponse? invoice = await _sales.GetInvoiceAsync(id, cancellationToken);

        return invoice is null ? this.NotFoundProblem("La venta no existe o no tiene factura.") : Ok(invoice);
    }

    /// <summary>Venta de mostrador (sin pedido).</summary>
    [HasPermission(SalesPermissions.Create)]
    [HttpPost]
    [AuditEntity("Venta", "VENDER")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> Sell(
        [FromBody] DirectSaleRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SaleResponse> result =
            await _sales.SellAsync(request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return Created(result);
    }

    [HasPermission(SalesPermissions.Create)]
    [HttpPost("desde-pedido/{orderId:int}")]
    [AuditEntity("Venta", "VENDER")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> InvoiceOrder(
        int orderId,
        [FromBody] InvoiceOrderRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SaleResponse> result =
            await _sales.InvoiceOrderAsync(orderId, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return Created(result);
    }

    /// <summary>Abono a una venta al crédito o con saldo.</summary>
    [HasPermission(SalesPermissions.Create)]
    [HttpPost("{id:int}/pagos")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> AddPayments(
        int id,
        [FromBody] AddPaymentsRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SaleResponse> result =
            await _sales.AddPaymentsAsync(id, request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(SalesPermissions.Cancel)]
    [HttpPost("{id:int}/anular")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> Annul(
        int id,
        [FromBody] AnnulSaleRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SaleResponse> result =
            await _sales.AnnulAsync(id, request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    private ActionResult<SaleResponse> Created(OperationResult<SaleResponse> result) =>
        result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
}
