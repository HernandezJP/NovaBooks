using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NovaBooks.Infrastructure.Services;

/// <summary>
/// Carpeta donde se guardan las portadas (Storage:ProductImagesPath,
/// por defecto App_Data/product-images dentro de NovaBooks.Api).
/// </summary>
public static class ProductImageStorage
{
    public static string GetDirectory(IConfiguration configuration, IHostEnvironment environment)
    {
        string configured = configuration["Storage:ProductImagesPath"] ?? "App_Data/product-images";

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
    }
}
