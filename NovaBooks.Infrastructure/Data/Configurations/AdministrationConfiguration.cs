using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.People;
using NovaBooks.Domain.Entities.Configuration;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class AdministrationConfiguration
{
    public static void ConfigureAdministration(
        this ModelBuilder modelBuilder)
    {
        ConfigurePuesto(modelBuilder);
        ConfigureSucursal(modelBuilder);
        ConfigureEmpleado(modelBuilder);
        ConfigureCliente(modelBuilder);
        ConfigureProveedor(modelBuilder);
    }

    private static void ConfigurePuesto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PUESTO>(builder =>
        {
            builder.ToTable("PB_PUESTO");

            builder.HasKey(x => x.PUE_Puesto);

            builder.Property(x => x.PUE_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.PUE_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.PUE_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.PUE_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.PUE_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureSucursal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_SUCURSAL>(builder =>
        {
            builder.ToTable("PB_SUCURSAL");

            builder.HasKey(x => x.SUC_Sucursal);

            builder.Property(x => x.SUC_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.SUC_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.SUC_Telefono)
                .HasMaxLength(25)
                .IsUnicode(false);

            builder.Property(x => x.SUC_Correo)
                .HasMaxLength(256);

            builder.Property(x => x.SUC_EsPrincipal)
                .HasDefaultValue(false);

            builder.Property(x => x.SUC_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.SUC_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => new
            {
                x.SUC_EmpresaId,
                x.SUC_Codigo
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.SUC_EmpresaId,
                x.SUC_Nombre
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.SUC_EmpresaId,
                x.SUC_EsPrincipal
            })
            .IsUnique()
            .HasFilter(
                "[SUC_EsPrincipal] = 1 AND [SUC_Activo] = 1");

            builder.HasOne(x => x.Empresa)
                .WithMany(x => x.Sucursales)
                .HasForeignKey(x => x.SUC_EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Direccion)
                .WithMany(x => x.Sucursales)
                .HasForeignKey(x => x.SUC_DireccionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureEmpleado(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_EMPLEADO>(builder =>
        {
            builder.ToTable("PB_EMPLEADO");

            builder.HasKey(x => x.EMP_Empleado);

            builder.Property(x => x.EMP_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.EMP_FechaContratacion)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.EMP_FechaFinalizacion)
                .HasColumnType("date");

            builder.Property(x => x.EMP_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.EMP_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.EMP_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.EMP_PersonaId)
                .IsUnique();

            builder.HasIndex(x => x.EMP_PuestoId);

            builder.HasIndex(x => x.EMP_SucursalId);

            builder.HasOne(x => x.Persona)
                .WithOne(x => x.Empleado)
                .HasForeignKey<PB_EMPLEADO>(
                    x => x.EMP_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Puesto)
                .WithMany(x => x.Empleados)
                .HasForeignKey(x => x.EMP_PuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sucursal)
                .WithMany(x => x.Empleados)
                .HasForeignKey(x => x.EMP_SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_EMPLEADO_FECHAS",
                    "[EMP_FechaFinalizacion] IS NULL OR " +
                    "[EMP_FechaFinalizacion] >= " +
                    "[EMP_FechaContratacion]");
            });
        });
    }

    private static void ConfigureCliente(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_CLIENTE>(builder =>
        {
            builder.ToTable("PB_CLIENTE");

            builder.HasKey(x => x.CLI_Cliente);

            builder.Property(x => x.CLI_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.CLI_LimiteCredito)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.CLI_DiasCredito)
                .HasDefaultValue(0);

            builder.Property(x => x.CLI_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.CLI_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.CLI_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.CLI_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.CLI_PersonaId)
                .IsUnique();

            builder.HasOne(x => x.Persona)
                .WithOne(x => x.Cliente)
                .HasForeignKey<PB_CLIENTE>(
                    x => x.CLI_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_CLIENTE_LIMITE_CREDITO",
                    "[CLI_LimiteCredito] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_CLIENTE_DIAS_CREDITO",
                    "[CLI_DiasCredito] >= 0");
            });
        });
    }

    private static void ConfigureProveedor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PROVEEDOR>(builder =>
        {
            builder.ToTable("PB_PROVEEDOR");

            builder.HasKey(x => x.PRO_Proveedor);

            builder.Property(x => x.PRO_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.PRO_LimiteCredito)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.PRO_DiasCredito)
                .HasDefaultValue(0);

            builder.Property(x => x.PRO_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.PRO_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.PRO_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.PRO_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.PRO_PersonaId)
                .IsUnique();

            builder.HasOne(x => x.Persona)
                .WithOne(x => x.Proveedor)
                .HasForeignKey<PB_PROVEEDOR>(
                    x => x.PRO_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_PROVEEDOR_LIMITE_CREDITO",
                    "[PRO_LimiteCredito] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_PROVEEDOR_DIAS_CREDITO",
                    "[PRO_DiasCredito] >= 0");
            });
        });
    }
}