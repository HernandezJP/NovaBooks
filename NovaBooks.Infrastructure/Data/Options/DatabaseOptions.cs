namespace NovaBooks.Infrastructure.Data.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public bool ResetAndSeedOnStartup { get; set; } = false;

    public bool SeedOnStartup { get; set; } = true;

    /// <summary>
    /// Datos de demostración del catálogo. Si no se configura, se cargan
    /// solo en el ambiente Development.
    /// </summary>
    public bool? SeedDemoData { get; set; }
}