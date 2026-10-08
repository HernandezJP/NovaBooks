using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovaBooks.Application.Common;
using NovaBooks.Application.Interfaces;
using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Infrastructure.Data;

namespace NovaBooks.Infrastructure.Services;

/// <summary>
/// Guarda las portadas en disco (Storage:ProductImagesPath, por defecto
/// App_Data/product-images) y el nombre del archivo en LIB_RutaImagen.
/// El tipo se valida por el contenido del archivo, no por su extensión.
/// </summary>
public sealed class ProductImageService : IProductImageService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductImageService> _logger;
    private readonly string _directory;

    public ProductImageService(
        AppDbContext context,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<ProductImageService> logger)
    {
        _context = context;
        _logger = logger;

        _directory = ProductImageStorage.GetDirectory(configuration, environment);
    }

    public async Task<OperationResult<string>> SaveAsync(
        int productId,
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        PB_LIBRO? book = await _context.Libros.FirstOrDefaultAsync(
            item => item.LIB_Libro == productId,
            cancellationToken);

        if (book is null)
        {
            return OperationResult<string>.Missing("El producto solicitado no existe.");
        }

        if (length <= 0)
        {
            return OperationResult<string>.Failure("Seleccione una imagen.");
        }

        if (length > IProductImageService.MaxBytes)
        {
            return OperationResult<string>.Failure("La imagen no puede superar los 2 MB.");
        }

        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > IProductImageService.MaxBytes)
        {
            return OperationResult<string>.Failure("La imagen no puede superar los 2 MB.");
        }

        string? extension = DetectExtension(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));

        if (extension is null)
        {
            return OperationResult<string>.Failure("Formato no admitido. Use una imagen JPG, PNG o WebP.");
        }

        Directory.CreateDirectory(_directory);

        string fileName = $"{productId}-{Guid.NewGuid():N}{extension}";
        string fullPath = Path.Combine(_directory, fileName);

        await File.WriteAllBytesAsync(fullPath, buffer.ToArray(), cancellationToken);

        string? previous = book.LIB_RutaImagen;

        book.LIB_RutaImagen = fileName;
        book.LIB_FechaModificacion = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            TryDelete(fileName);
            throw;
        }

        TryDelete(previous);

        return OperationResult<string>.Success(fileName);
    }

    public async Task<OperationResult<bool>> DeleteAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        PB_LIBRO? book = await _context.Libros.FirstOrDefaultAsync(
            item => item.LIB_Libro == productId,
            cancellationToken);

        if (book is null)
        {
            return OperationResult<bool>.Missing("El producto solicitado no existe.");
        }

        string? previous = book.LIB_RutaImagen;

        if (previous is null)
        {
            return OperationResult<bool>.Success(true);
        }

        book.LIB_RutaImagen = null;
        book.LIB_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        TryDelete(previous);

        return OperationResult<bool>.Success(true);
    }

    public async Task<ProductImageFile?> GetAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        string? fileName =
            await _context.Libros
                .AsNoTracking()
                .Where(item => item.LIB_Libro == productId)
                .Select(item => item.LIB_RutaImagen)
                .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(fileName) || !IsSafeFileName(fileName))
        {
            return null;
        }

        string fullPath = Path.Combine(_directory, fileName);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        string contentType = Path.GetExtension(fileName) switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return new ProductImageFile(fullPath, contentType, fileName);
    }

    private static string? DetectExtension(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return ".jpg";
        }

        if (bytes.Length >= 8 &&
            bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        if (bytes.Length >= 12 &&
            bytes[..4].SequenceEqual("RIFF"u8) &&
            bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }

    private static bool IsSafeFileName(string fileName) =>
        fileName == Path.GetFileName(fileName) &&
        fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    private void TryDelete(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || !IsSafeFileName(fileName))
        {
            return;
        }

        try
        {
            File.Delete(Path.Combine(_directory, fileName));
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "No fue posible eliminar la imagen {FileName}.", fileName);
        }
    }
}
