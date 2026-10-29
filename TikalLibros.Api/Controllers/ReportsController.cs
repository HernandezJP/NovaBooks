using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Reportes. Los costos y márgenes de los libros más vendidos solo se
/// incluyen con Reportes.Rentabilidad o Inventario.VerCostos; cada reporte de
/// gestión exige su propio permiso (compras, caja, rentabilidad).
/// </summary>
[ApiController]
[Route("api/reportes")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HasPermission(ReportPermissions.Sales)]
    [HttpGet("ventas")]
    [ProducesResponseType(typeof(SalesReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SalesReportResponse>> GetSales(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        OperationResult<SalesReportResponse> result = await _reports.GetSalesAsync(query, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(ReportPermissions.Sales)]
    [HttpGet("libros-mas-vendidos")]
    [ProducesResponseType(typeof(TopBooksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TopBooksResponse>> GetTopBooks(
        [FromQuery] TopBooksQuery query,
        CancellationToken cancellationToken)
    {
        bool includeCosts = this.HasPermission(ReportPermissions.Profitability) || this.HasPermission(InventoryPermissions.ViewCosts);

        OperationResult<TopBooksResponse> result = await _reports.GetTopBooksAsync(query, includeCosts, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(ReportPermissions.Inventory)]
    [HttpGet("inventario-valorizado")]
    [ProducesResponseType(typeof(InventoryValuationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryValuationResponse>> GetInventoryValuation(
        [FromQuery] InventoryValuationQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _reports.GetInventoryValuationAsync(query, cancellationToken));
    }

    [HasAnyPermission(ReportPermissions.Sales, OrderPermissions.Approve)]
    [HttpGet("pedidos-pendientes")]
    [ProducesResponseType(typeof(PendingOrdersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PendingOrdersResponse>> GetPendingOrders(CancellationToken cancellationToken)
    {
        return Ok(await _reports.GetPendingOrdersAsync(cancellationToken));
    }

    [HasPermission(ReportPermissions.Purchases)]
    [HttpGet("compras")]
    [ProducesResponseType(typeof(PurchasesReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PurchasesReportResponse>> GetPurchases(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        OperationResult<PurchasesReportResponse> result = await _reports.GetPurchasesAsync(query, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(ReportPermissions.Cash)]
    [HttpGet("caja")]
    [ProducesResponseType(typeof(CashReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CashReportResponse>> GetCash(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        OperationResult<CashReportResponse> result = await _reports.GetCashAsync(query, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(ReportPermissions.Profitability)]
    [HttpGet("rentabilidad")]
    [ProducesResponseType(typeof(ProfitabilityReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfitabilityReportResponse>> GetProfitability(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        OperationResult<ProfitabilityReportResponse> result = await _reports.GetProfitabilityAsync(query, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(ReportPermissions.Sales)]
    [HttpGet("cuentas-por-cobrar")]
    [ProducesResponseType(typeof(ReceivablesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceivablesResponse>> GetReceivables(
        [FromQuery] ReceivablesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _reports.GetReceivablesAsync(query, cancellationToken));
    }
}
