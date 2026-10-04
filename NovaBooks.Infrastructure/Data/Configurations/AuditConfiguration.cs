using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Audit;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class AuditConfiguration
{
    public static void ConfigureAudit(this ModelBuilder modelBuilder)
    {
        ConfigureAuditoria(modelBuilder);
        ConfigureAuditoriaDetalle(modelBuilder);
        ConfigureHistorialAcceso(modelBuilder);
    }

    private static void ConfigureAuditoria(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_AUDITORIA>(builder =>
        {
            builder.ToTable("PB_AUDITORIA");

            builder.HasKey(x => x.AUD_Auditoria);

            builder.Property(x => x.AUD_Accion)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.AUD_Entidad)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.AUD_EntidadId)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.Property(x => x.AUD_Descripcion)
                .HasMaxLength(1000);

            builder.Property(x => x.AUD_DireccionIp)
                .HasMaxLength(45)
                .IsUnicode(false);

            builder.Property(x => x.AUD_UserAgent)
                .HasMaxLength(1000);

            builder.Property(x => x.AUD_Ruta)
                .HasMaxLength(1000);

            builder.Property(x => x.AUD_MetodoHttp)
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.Property(x => x.AUD_CorrelacionId)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.Property(x => x.AUD_Fecha)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.AUD_Fecha);

            builder.HasIndex(x => x.AUD_UsuarioId);

            builder.HasIndex(x => new
            {
                x.AUD_Entidad,
                x.AUD_EntidadId
            });

            builder.HasIndex(x => x.AUD_CorrelacionId)
                .HasFilter("[AUD_CorrelacionId] IS NOT NULL");

            builder.HasIndex(x => new
            {
                x.AUD_Accion,
                x.AUD_Fecha
            });
        });
    }

    private static void ConfigureAuditoriaDetalle(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_AUDITORIA_DETALLE>(builder =>
        {
            builder.ToTable("PB_AUDITORIA_DETALLE");

            builder.HasKey(x => x.ADE_AuditoriaDetalle);

            builder.Property(x => x.ADE_Campo)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.ADE_ValorAnterior)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.ADE_ValorNuevo)
                .HasColumnType("nvarchar(max)");

            builder.HasIndex(x => x.ADE_AuditoriaId);

            builder.HasOne(x => x.Auditoria)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.ADE_AuditoriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureHistorialAcceso(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_HISTORIAL_ACCESO>(builder =>
        {
            builder.ToTable("PB_HISTORIAL_ACCESO");

            builder.HasKey(x => x.HAC_HistorialAcceso);

            builder.Property(x => x.HAC_NombreUsuario)
                .HasMaxLength(256);

            builder.Property(x => x.HAC_Fecha)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.HAC_Exitoso)
                .HasDefaultValue(false);

            builder.Property(x => x.HAC_MotivoFallo)
                .HasMaxLength(500);

            builder.Property(x => x.HAC_DireccionIp)
                .HasMaxLength(45)
                .IsUnicode(false);

            builder.Property(x => x.HAC_UserAgent)
                .HasMaxLength(1000);

            builder.Property(x => x.HAC_CorrelacionId)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.HasIndex(x => x.HAC_Fecha);

            builder.HasIndex(x => x.HAC_UsuarioId);

            builder.HasIndex(x => new
            {
                x.HAC_NombreUsuario,
                x.HAC_Fecha
            });

            builder.HasIndex(x => new
            {
                x.HAC_Exitoso,
                x.HAC_Fecha
            });

            builder.HasIndex(x => x.HAC_CorrelacionId)
                .HasFilter("[HAC_CorrelacionId] IS NOT NULL");
        });
    }
}