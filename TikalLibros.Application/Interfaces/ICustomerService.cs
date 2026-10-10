using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Customers;

namespace TikalLibros.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResponse<CustomerListItemResponse>> GetPagedAsync(
        CustomerQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<CustomerCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<CustomerResponse>> CreateAsync(
        SaveCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CustomerResponse>> UpdateAsync(
        int id,
        SaveCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CustomerResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
