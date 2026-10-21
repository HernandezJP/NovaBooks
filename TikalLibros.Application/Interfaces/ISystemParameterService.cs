using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Configuration;

namespace TikalLibros.Application.Interfaces;

public interface ISystemParameterService
{
    Task<IReadOnlyCollection<SystemParameterResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Valor numérico del parámetro; si no existe o no es válido, el valor inicial.</summary>
    Task<decimal> GetDecimalAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Valor de texto del parámetro; si no existe, el valor inicial.</summary>
    Task<string> GetStringAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<OperationResult<SystemParameterResponse>> UpdateAsync(
        string key,
        UpdateSystemParameterRequest request,
        CancellationToken cancellationToken = default);
}
