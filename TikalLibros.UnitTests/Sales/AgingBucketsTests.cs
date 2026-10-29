using TikalLibros.Application.DTOs.Reports;

namespace TikalLibros.UnitTests.Sales;

/// <summary>Tramos de antigüedad de las cuentas por cobrar.</summary>
public sealed class AgingBucketsTests
{
    [Theory]
    [InlineData(-5, AgingBuckets.Current)]
    [InlineData(0, AgingBuckets.Current)]
    [InlineData(1, AgingBuckets.Days30)]
    [InlineData(30, AgingBuckets.Days30)]
    [InlineData(31, AgingBuckets.Days60)]
    [InlineData(60, AgingBuckets.Days60)]
    [InlineData(61, AgingBuckets.Days90)]
    [InlineData(90, AgingBuckets.Days90)]
    [InlineData(91, AgingBuckets.Over90)]
    public void DiasDeAtraso_CaenEnSuTramo(int days, string expected)
    {
        Assert.Equal(expected, AgingBuckets.For(days));
    }
}
