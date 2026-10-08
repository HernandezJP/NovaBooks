using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Editorials;

namespace NovaBooks.Application.Interfaces;

public interface IEditorialService
{
    Task<PagedResponse<EditorialResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<EditorialResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<EditorialResponse>> CreateAsync(
        SaveEditorialRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<EditorialResponse>> UpdateAsync(
        int id,
        SaveEditorialRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<EditorialResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
