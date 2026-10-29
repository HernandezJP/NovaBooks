namespace TikalLibros.Application.Common;

/// <summary>
/// Hora del negocio (Guatemala, UTC−6, sin horario de verano). Las fechas se
/// guardan en UTC; los días que elige el usuario ("hoy", "del 1 al 14") se
/// interpretan en la hora local de la librería.
/// </summary>
public static class BusinessTime
{
    public static readonly TimeZoneInfo Zone = FindZone();

    /// <summary>Fecha actual en Guatemala.</summary>
    public static DateOnly Today => ToLocalDate(DateTime.UtcNow);

    /// <summary>Fecha local de un instante guardado en UTC.</summary>
    public static DateOnly ToLocalDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));

    /// <summary>Inicio del día local, expresado en UTC (para filtrar "desde").</summary>
    public static DateTime StartOfDayUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), Zone);

    /// <summary>Inicio del día siguiente, en UTC (para filtrar "hasta", exclusivo).</summary>
    public static DateTime EndOfDayUtcExclusive(DateOnly date) => StartOfDayUtc(date.AddDays(1));

    private static TimeZoneInfo FindZone()
    {
        foreach (string id in new[] { "America/Guatemala", "Central America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("America/Guatemala", TimeSpan.FromHours(-6), "Guatemala", "Guatemala");
    }
}
