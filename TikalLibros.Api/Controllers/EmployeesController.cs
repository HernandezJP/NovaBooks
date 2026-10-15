using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Employees;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Empleados de la librería. El usuario de cada empleado se crea o vincula
/// desde /api/usuarios (EmployeeId).
/// </summary>
[ApiController]
[Route("api/empleados")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HasPermission(EmployeePermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EmployeeListItemResponse>>> GetPaged(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? positionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _employeeService.GetPagedAsync(search, isActive, positionId, page, pageSize, cancellationToken));
    }

    [HasPermission(EmployeePermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        EmployeeResponse? employee = await _employeeService.GetByIdAsync(id, cancellationToken);

        return employee is null
            ? this.NotFoundProblem("El empleado solicitado no existe.")
            : Ok(employee);
    }

    [HasAnyPermission(EmployeePermissions.View, EmployeePermissions.Create, EmployeePermissions.Update)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(EmployeeCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _employeeService.GetCatalogsAsync(cancellationToken));
    }

    [HasPermission(EmployeePermissions.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Create(
        [FromBody] SaveEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<EmployeeResponse> result = await _employeeService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(EmployeePermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Update(
        int id,
        [FromBody] SaveEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<EmployeeResponse> result = await _employeeService.UpdateAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(EmployeePermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeEmployeeStatusRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<EmployeeResponse> result = await _employeeService.ChangeStatusAsync(
            id, request.IsActive, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
