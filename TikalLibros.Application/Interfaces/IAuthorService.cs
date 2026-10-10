using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Authors;

namespace TikalLibros.Application.Interfaces;

public interface IAuthorService
{
    Task<PagedResponse<AuthorResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AuthorResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<AuthorResponse>> CreateAsync(
        SaveAuthorRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<AuthorResponse>> UpdateAsync(
        int id,
        SaveAuthorRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<AuthorResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
