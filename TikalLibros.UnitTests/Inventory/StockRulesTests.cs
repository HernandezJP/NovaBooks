using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Infrastructure.Services.Inventory;

namespace TikalLibros.UnitTests.Inventory;

public sealed class StockRulesTests
{
    [Theory]
    [InlineData(0, 0, 0, StockStatus.OutOfStock)]
    [InlineData(0, 5, 10, StockStatus.OutOfStock)]
    [InlineData(5, 5, 10, StockStatus.BelowMinimum)]
    [InlineData(6, 5, 10, StockStatus.Normal)]
    [InlineData(10, 5, 10, StockStatus.Normal)]
    [InlineData(11, 5, 10, StockStatus.AboveMaximum)]
    [InlineData(500, 0, 0, StockStatus.Normal)]
    public void Classify_ComparaConMinimoYMaximo(int available, int minimum, int maximum, string expected)
    {
        Assert.Equal(expected, InventoryService.Classify(available, minimum, maximum));
    }

    [Theory]
    [InlineData(10, 5, false)]
    [InlineData(10, 0, true)]
    [InlineData(10, 10, true)]
    [InlineData(-1, 0, false)]
    public void Niveles_MaximoDebeSerCeroOMayorQueElMinimo(int minimum, int maximum, bool valid)
    {
        SaveStockLevelsRequest request = new() { WarehouseId = 1, BookId = 1, Minimum = minimum, Maximum = maximum };
        List<ValidationResult> results = [];

        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.Equal(valid, results.Count == 0);
    }

    [Theory]
    [InlineData("Estante A-3", true)]
    [InlineData("Bodega 2 / Pasillo #4", true)]
    [InlineData("Estante <A>", false)]
    [InlineData("-Estante", false)]
    public void Niveles_UbicacionSoloCaracteresPermitidos(string location, bool valid)
    {
        SaveStockLevelsRequest request = new() { WarehouseId = 1, BookId = 1, Location = location };
        List<ValidationResult> results = [];

        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.Equal(valid, results.Count == 0);
    }
}
