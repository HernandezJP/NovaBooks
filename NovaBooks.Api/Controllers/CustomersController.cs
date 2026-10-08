using Microsoft.AspNetCore.Mvc;
using NovaBooks.Api.Common;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Customers;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HasPermission(CustomerPermissions.View)]
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<CustomerListItemResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<CustomerListItemResponse>>> GetPaged(
            [FromQuery] CustomerQuery query,
            CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetPagedAsync(
            query,
            cancellationToken));
    }

    [HasPermission(CustomerPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(CustomerResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        CustomerResponse? customer =
            await _customerService.GetByIdAsync(
                id,
                cancellationToken);

        if (customer is null)
        {
            return this.NotFoundProblem(
                "El cliente solicitado no existe.");
        }

        return Ok(customer);
    }

    [HasAnyPermission(
        CustomerPermissions.View,
        CustomerPermissions.Create,
        CustomerPermissions.Update)]
    [HttpGet("catalogos")]
    [ProducesResponseType(
        typeof(CustomerCatalogsResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerCatalogsResponse>> GetCatalogs(
        CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetCatalogsAsync(
            cancellationToken));
    }

    [HasPermission(CustomerPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(
        typeof(CustomerResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<CustomerResponse> result =
            await _customerService.CreateAsync(
                request,
                cancellationToken);

        if (!result.Succeeded)
        {
            return this.OperationProblem(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Data!.Id
            },
            result.Data);
    }

    [HasPermission(CustomerPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(CustomerResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Update(
        int id,
        [FromBody] SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(
            await _customerService.UpdateAsync(
                id,
                request,
                cancellationToken));
    }

    /// <summary>
    /// Desactivación lógica: el cliente se conserva para históricos.
    /// </summary>
    [HasPermission(CustomerPermissions.Disable)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        OperationResult<CustomerResponse> result =
            await _customerService.ChangeStatusAsync(
                id,
                isActive: false,
                cancellationToken);

        return result.Succeeded
            ? NoContent()
            : this.OperationProblem(result);
    }

    [HasPermission(CustomerPermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(
        typeof(CustomerResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(
            await _customerService.ChangeStatusAsync(
                id,
                request.IsActive,
                cancellationToken));
    }

    private ActionResult<CustomerResponse> FromResult(
        OperationResult<CustomerResponse> result)
    {
        return result.Succeeded
            ? Ok(result.Data)
            : this.OperationProblem(result);
    }
}
