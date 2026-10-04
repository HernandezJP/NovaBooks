using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Audit;
using NovaBooks.Infrastructure.Data.Identity;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class IdentityConfiguration
{
    public static void ConfigureIdentity(this ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureIdentityTables(modelBuilder);
        ConfigureAuditUserRelationships(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.ToTable("SEG_USUARIO");

            builder.Property(x => x.USU_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.USU_DebeCambiarPassword)
                .HasDefaultValue(true);

            builder.Property(x => x.USU_FechaUltimoAcceso)
                .HasColumnType("datetime2");

            builder.Property(x => x.USU_FechaCambioPassword)
                .HasColumnType("datetime2");

            builder.Property(x => x.USU_FechaCreacion)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.USU_FechaModificacion)
                .HasColumnType("datetime2");

            builder.HasIndex(x => x.USU_EmpleadoId)
                .IsUnique()
                .HasFilter("[USU_EmpleadoId] IS NOT NULL");

            builder.HasIndex(x => new
            {
                x.USU_Activo,
                x.UserName
            });

            builder.HasOne(x => x.Empleado)
                .WithOne()
                .HasForeignKey<ApplicationUser>(
                    x => x.USU_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.USU_CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.USU_ModificadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationRole>(builder =>
        {
            builder.ToTable("SEG_ROL");

            builder.Property(x => x.ROL_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.ROL_EsSistema)
                .HasDefaultValue(false);

            builder.Property(x => x.ROL_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.ROL_FechaCreacion)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.ROL_FechaModificacion)
                .HasColumnType("datetime2");
        });
    }

    private static void ConfigureIdentityTables(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUserRole<int>>(builder =>
        {
            builder.ToTable("SEG_USUARIO_ROL");
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(builder =>
        {
            builder.ToTable("SEG_USUARIO_CLAIM");

            builder.Property(x => x.ClaimType)
                .HasMaxLength(250);

            builder.Property(x => x.ClaimValue)
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(builder =>
        {
            builder.ToTable("SEG_ROL_CLAIM");

            builder.Property(x => x.ClaimType)
                .HasMaxLength(250);

            builder.Property(x => x.ClaimValue)
                .HasMaxLength(1000);

            builder.HasIndex(x => new
            {
                x.RoleId,
                x.ClaimType,
                x.ClaimValue
            }).IsUnique();
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(builder =>
        {
            builder.ToTable("SEG_USUARIO_LOGIN");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(builder =>
        {
            builder.ToTable("SEG_USUARIO_TOKEN");
        });
    }

    private static void ConfigureAuditUserRelationships(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_AUDITORIA>(builder =>
        {
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.AUD_UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PB_HISTORIAL_ACCESO>(builder =>
        {
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.HAC_UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}