namespace NovaBooks.Infrastructure.Data.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public bool ResetAndSeedOnStartup { get; set; } = false;

    public bool SeedOnStartup { get; set; } = true;
}