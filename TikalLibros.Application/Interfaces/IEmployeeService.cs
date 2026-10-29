using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Employees;

namespace TikalLibros.Application.Interfaces;

public interface IEmployeeService
{
    Task<PagedResponse<EmployeeListItemResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int? positionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<EmployeeResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<EmployeeCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<EmployeeResponse>> CreateAsync(
        SaveEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<EmployeeResponse>> UpdateAsync(
        int id,
        SaveEmployeeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Al desactivar al empleado también se desactiva su usuario, lo que
    /// invalida sus sesiones abiertas.
    /// </summary>
    Task<OperationResult<EmployeeResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
