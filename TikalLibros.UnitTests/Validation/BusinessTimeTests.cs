using TikalLibros.Application.Common;

namespace TikalLibros.UnitTests.Validation;

/// <summary>Las fechas se guardan en UTC y se interpretan en hora de Guatemala (UTC−6).</summary>
public sealed class BusinessTimeTests
{
    [Fact]
    public void InicioDelDia_EsLasSeisDeLaMananaUtc()
    {
        Assert.Equal(new DateTime(2026, 10, 14, 6, 0, 0), BusinessTime.StartOfDayUtc(new DateOnly(2026, 10, 14)));
        Assert.Equal(new DateTime(2026, 10, 15, 6, 0, 0), BusinessTime.EndOfDayUtcExclusive(new DateOnly(2026, 10, 14)));
    }

    [Theory]
    [InlineData("2026-10-15T02:29:00", "2026-10-14")] // 20:29 del 14 en Guatemala
    [InlineData("2026-10-15T05:59:59", "2026-10-14")]
    [InlineData("2026-10-15T06:00:00", "2026-10-15")]
    public void FechaLocal_DeUnInstanteUtc(string utc, string local)
    {
        Assert.Equal(DateOnly.Parse(local), BusinessTime.ToLocalDate(DateTime.Parse(utc)));
    }

    [Fact]
    public void MovimientoDeLaNoche_EntraEnElFiltroDeSuDiaLocal()
    {
        DateTime movement = new(2026, 10, 15, 2, 29, 0); // 20:29 del 14 en Guatemala
        DateOnly day = new(2026, 10, 14);

        Assert.True(movement >= BusinessTime.StartOfDayUtc(day) && movement < BusinessTime.EndOfDayUtcExclusive(day));
    }
}
