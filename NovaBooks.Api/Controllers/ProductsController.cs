using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaBooks.Api.Common;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Products;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductImageService _imageService;

    public ProductsController(
        IProductService productService,
        IProductImageService imageService)
    {
        _productService = productService;
        _imageService = imageService;
    }

    /// <summary>
    /// Los costos solo se incluyen con Catalogo.AdministrarPrecios.
    /// </summary>
    private bool CanManagePrices =>
        this.HasPermission(CatalogPermissions.ManagePrices);

    [HasPermission(CatalogPermissions.View)]
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<ProductListItemResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<ProductListItemResponse>>> GetPaged(
            [FromQuery] ProductQuery query,
            CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetPagedAsync(
            query,
            CanManagePrices,
            cancellationToken));
    }

    [HasPermission(CatalogPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        ProductResponse? product =
            await _productService.GetByIdAsync(
                id,
                CanManagePrices,
                cancellationToken);

        if (product is null)
        {
            return this.NotFoundProblem(
                "El producto solicitado no existe.");
        }

        return Ok(product);
    }

    [HasAnyPermission(
        CatalogPermissions.View,
        CatalogPermissions.Create,
        CatalogPermissions.Update)]
    [HttpGet("catalogos")]
    [ProducesResponseType(
        typeof(ProductCatalogsResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductCatalogsResponse>> GetCatalogs(
        CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetCatalogsAsync(
            cancellationToken));
    }

    [HasPermission(CatalogPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ChangesPricing && !CanManagePrices)
        {
            return PricingForbidden();
        }

        OperationResult<ProductResponse> result =
            await _productService.CreateAsync(
                request,
                CanManagePrices,
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

    [HasPermission(CatalogPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        [FromBody] SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ChangesPricing && !CanManagePrices)
        {
            return PricingForbidden();
        }

        return FromResult(
            await _productService.UpdateAsync(
                id,
                request,
                CanManagePrices,
                cancellationToken));
    }

    /// <summary>
    /// Desactivación lógica: el libro se conserva para movimientos e históricos.
    /// </summary>
    [HasPermission(CatalogPermissions.Disable)]
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
        OperationResult<ProductResponse> result =
            await _productService.ChangeStatusAsync(
                id,
                isActive: false,
                CanManagePrices,
                cancellationToken);

        return result.Succeeded
            ? NoContent()
            : this.OperationProblem(result);
    }

    [HasPermission(CatalogPermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(
            await _productService.ChangeStatusAsync(
                id,
                request.IsActive,
                CanManagePrices,
                cancellationToken));
    }

    /// <summary>
    /// Portada del libro. Es pública para que el navegador pueda mostrarla
    /// en una etiqueta img sin token; no contiene información sensible.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}/imagen")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(int id, CancellationToken cancellationToken)
    {
        ProductImageFile? image = await _imageService.GetAsync(id, cancellationToken);

        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=604800";
        Response.Headers.XContentTypeOptions = "nosniff";

        return PhysicalFile(image.Path, image.ContentType);
    }

    [HasPermission(CatalogPermissions.Update)]
    [HttpPost("{id:int}/imagen")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> UploadImage(
        int id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return this.OperationProblem(OperationResult<string>.Failure("Seleccione una imagen."));
        }

        await using Stream stream = file.OpenReadStream();

        OperationResult<string> result =
            await _imageService.SaveAsync(id, stream, file.Length, cancellationToken);

        if (!result.Succeeded)
        {
            return this.OperationProblem(result);
        }

        return Ok(await _productService.GetByIdAsync(id, CanManagePrices, cancellationToken));
    }

    [HasPermission(CatalogPermissions.Update)]
    [HttpDelete("{id:int}/imagen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken)
    {
        OperationResult<bool> result = await _imageService.DeleteAsync(id, cancellationToken);

        return result.Succeeded ? NoContent() : this.OperationProblem(result);
    }

    private ObjectResult PricingForbidden()
    {
        return this.ForbiddenProblem(
            "Se requiere el permiso Catalogo.AdministrarPrecios " +
            "para enviar precios o costo de referencia.");
    }

    private ActionResult<ProductResponse> FromResult(
        OperationResult<ProductResponse> result)
    {
        return result.Succeeded
            ? Ok(result.Data)
            : this.OperationProblem(result);
    }
}
