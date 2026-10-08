using NovaBooks.Application.Common;

namespace NovaBooks.Application.Interfaces;

public sealed record ProductImageFile(string Path, string ContentType, string Version);

/// <summary>
/// Imagen de portada de un libro (JPG, PNG o WebP).
/// </summary>
public interface IProductImageService
{
    public const long MaxBytes = 2 * 1024 * 1024;

    /// <summary>Guarda la imagen y devuelve su versión (nombre de archivo).</summary>
    Task<OperationResult<string>> SaveAsync(
        int productId,
        Stream content,
        long length,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductImageFile?> GetAsync(
        int productId,
        CancellationToken cancellationToken = default);
}
