using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NovaBooks.IntegrationTests.Infrastructure;

/// <summary>
/// Adapta, solo para las pruebas, las partes del modelo exclusivas de SQL
/// Server: SYSUTCDATETIME() como valor por defecto y columnas rowversion.
/// El modelo de producción no cambia.
/// </summary>
public sealed class SqliteModelCustomizer : RelationalModelCustomizer
{
    public SqliteModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // SQLite usa LENGTH en lugar de LEN.
            foreach (var constraint in entityType.GetCheckConstraints().ToList())
            {
                if (constraint.Sql.Contains("LEN(", StringComparison.OrdinalIgnoreCase))
                {
                    string name = constraint.ModelName;
                    string sql = constraint.Sql.Replace("LEN(", "LENGTH(", StringComparison.OrdinalIgnoreCase);
                    entityType.RemoveCheckConstraint(name);
                    entityType.AddCheckConstraint(name, sql);
                }
            }

            foreach (var property in entityType.GetProperties())
            {
                string? columnType = property.GetColumnType();
                if (columnType is not null && columnType.Contains("(max)", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetColumnType("TEXT");
                }
                else if (Nullable.GetUnderlyingType(property.ClrType) == typeof(decimal) || property.ClrType == typeof(decimal))
                {
                    // Afinidad numérica para que los CHECK comparen números y no texto.
                    property.SetColumnType("NUMERIC");
                }
                else if (string.Equals(columnType, "rowversion", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetColumnType("BLOB");
                }

                if (string.Equals(property.GetDefaultValueSql(), "SYSUTCDATETIME()", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }

                // SQLite no genera rowversion: se trata como columna normal.
                if (property.IsConcurrencyToken && property.ValueGenerated == ValueGenerated.OnAddOrUpdate)
                {
                    property.ValueGenerated = ValueGenerated.Never;
                    property.IsConcurrencyToken = false;
                    property.IsNullable = true;
                }
            }
        }
    }
}
