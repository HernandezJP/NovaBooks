using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Audit;
using NovaBooks.Domain.Entities.Cash;
using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Domain.Entities.Configuration;
using NovaBooks.Domain.Entities.Geography;
using NovaBooks.Domain.Entities.Inventory;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.People;
using NovaBooks.Domain.Entities.Purchasing;
using NovaBooks.Domain.Entities.Sales;
using NovaBooks.Infrastructure.Data.Configurations;
using NovaBooks.Infrastructure.Data.Identity;

namespace NovaBooks.Infrastructure.Data;

public class AppDbContext
    : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        int,
        IdentityUserClaim<int>,
        IdentityUserRole<int>,
        IdentityUserLogin<int>,
        IdentityRoleClaim<int>,
        IdentityUserToken<int>>
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Geography

    public DbSet<GEO_PAIS> Paises => Set<GEO_PAIS>();

    public DbSet<GEO_DEPARTAMENTO> Departamentos =>
        Set<GEO_DEPARTAMENTO>();

    public DbSet<GEO_MUNICIPIO> Municipios =>
        Set<GEO_MUNICIPIO>();

    // People

    public DbSet<PB_TIPO_PERSONA> TiposPersona =>
        Set<PB_TIPO_PERSONA>();

    public DbSet<PB_PERSONA> Personas =>
        Set<PB_PERSONA>();

    public DbSet<PB_PERSONA_NATURAL> PersonasNaturales =>
        Set<PB_PERSONA_NATURAL>();

    public DbSet<PB_PERSONA_JURIDICA> PersonasJuridicas =>
        Set<PB_PERSONA_JURIDICA>();

    public DbSet<PB_TIPO_IDENTIFICACION> TiposIdentificacion =>
        Set<PB_TIPO_IDENTIFICACION>();

    public DbSet<PB_PERSONA_IDENTIFICACION> PersonasIdentificaciones =>
        Set<PB_PERSONA_IDENTIFICACION>();

    public DbSet<PB_TIPO_TELEFONO> TiposTelefono =>
        Set<PB_TIPO_TELEFONO>();

    public DbSet<PB_PERSONA_TELEFONO> PersonasTelefonos =>
        Set<PB_PERSONA_TELEFONO>();

    public DbSet<PB_PERSONA_CORREO> PersonasCorreos =>
        Set<PB_PERSONA_CORREO>();

    public DbSet<PB_TIPO_DIRECCION> TiposDireccion =>
        Set<PB_TIPO_DIRECCION>();

    public DbSet<PB_DIRECCION> Direcciones =>
        Set<PB_DIRECCION>();

    public DbSet<PB_PERSONA_DIRECCION> PersonasDirecciones =>
        Set<PB_PERSONA_DIRECCION>();

    // Administration

    public DbSet<PB_PUESTO> Puestos =>
        Set<PB_PUESTO>();

    public DbSet<PB_SUCURSAL> Sucursales =>
        Set<PB_SUCURSAL>();

    public DbSet<PB_EMPLEADO> Empleados =>
        Set<PB_EMPLEADO>();

    public DbSet<PB_CLIENTE> Clientes =>
        Set<PB_CLIENTE>();

    public DbSet<PB_PROVEEDOR> Proveedores =>
        Set<PB_PROVEEDOR>();

    // Configuration

    public DbSet<PB_EMPRESA> Empresas =>
        Set<PB_EMPRESA>();

    public DbSet<PB_PARAMETRO_SISTEMA> ParametrosSistema =>
        Set<PB_PARAMETRO_SISTEMA>();

    public DbSet<PB_SECUENCIA_DOCUMENTO> SecuenciasDocumento =>
        Set<PB_SECUENCIA_DOCUMENTO>();

    // Catalog

    public DbSet<PB_AUTOR> Autores =>
        Set<PB_AUTOR>();

    public DbSet<PB_EDITORIAL> Editoriales =>
        Set<PB_EDITORIAL>();

    public DbSet<PB_CATEGORIA> Categorias =>
        Set<PB_CATEGORIA>();

    public DbSet<PB_IDIOMA> Idiomas =>
        Set<PB_IDIOMA>();

    public DbSet<PB_FORMATO_LIBRO> FormatosLibro =>
        Set<PB_FORMATO_LIBRO>();

    public DbSet<PB_IMPUESTO> Impuestos =>
        Set<PB_IMPUESTO>();

    public DbSet<PB_LIBRO> Libros =>
        Set<PB_LIBRO>();

    public DbSet<PB_LIBRO_AUTOR> LibrosAutores =>
        Set<PB_LIBRO_AUTOR>();

    public DbSet<PB_LIBRO_CATEGORIA> LibrosCategorias =>
        Set<PB_LIBRO_CATEGORIA>();

    public DbSet<PB_LISTA_PRECIO> ListasPrecio =>
        Set<PB_LISTA_PRECIO>();

    public DbSet<PB_LIBRO_PRECIO> LibrosPrecios =>
        Set<PB_LIBRO_PRECIO>();

    // Payments

    public DbSet<PB_METODO_PAGO> MetodosPago =>
        Set<PB_METODO_PAGO>();

    public DbSet<PB_MONEDA> Monedas =>
        Set<PB_MONEDA>();

    // Purchasing

    public DbSet<PB_ESTADO_ORDEN_COMPRA> EstadosOrdenCompra =>
        Set<PB_ESTADO_ORDEN_COMPRA>();

    public DbSet<PB_ORDEN_COMPRA> OrdenesCompra =>
        Set<PB_ORDEN_COMPRA>();

    public DbSet<PB_ORDEN_COMPRA_DETALLE> OrdenesCompraDetalles =>
        Set<PB_ORDEN_COMPRA_DETALLE>();

    public DbSet<PB_ESTADO_RECEPCION_COMPRA> EstadosRecepcionCompra =>
        Set<PB_ESTADO_RECEPCION_COMPRA>();

    public DbSet<PB_RECEPCION_COMPRA> RecepcionesCompra =>
        Set<PB_RECEPCION_COMPRA>();

    public DbSet<PB_RECEPCION_COMPRA_DETALLE> RecepcionesCompraDetalles =>
        Set<PB_RECEPCION_COMPRA_DETALLE>();

    // Inventory

    public DbSet<PB_ALMACEN> Almacenes =>
        Set<PB_ALMACEN>();

    public DbSet<PB_EXISTENCIA> Existencias =>
        Set<PB_EXISTENCIA>();

    public DbSet<PB_LOTE> Lotes =>
        Set<PB_LOTE>();

    public DbSet<PB_TIPO_MOVIMIENTO_INVENTARIO>
        TiposMovimientoInventario =>
        Set<PB_TIPO_MOVIMIENTO_INVENTARIO>();

    public DbSet<PB_MOVIMIENTO_INVENTARIO>
        MovimientosInventario =>
        Set<PB_MOVIMIENTO_INVENTARIO>();

    public DbSet<PB_MOVIMIENTO_INVENTARIO_DETALLE>
        MovimientosInventarioDetalles =>
        Set<PB_MOVIMIENTO_INVENTARIO_DETALLE>();

    public DbSet<PB_MOTIVO_AJUSTE> MotivosAjuste =>
        Set<PB_MOTIVO_AJUSTE>();

    public DbSet<PB_AJUSTE_INVENTARIO> AjustesInventario =>
        Set<PB_AJUSTE_INVENTARIO>();

    public DbSet<PB_AJUSTE_INVENTARIO_DETALLE>
        AjustesInventarioDetalles =>
        Set<PB_AJUSTE_INVENTARIO_DETALLE>();

    // Sales

    public DbSet<PB_TIPO_VENTA> TiposVenta =>
        Set<PB_TIPO_VENTA>();

    public DbSet<PB_ESTADO_VENTA> EstadosVenta =>
        Set<PB_ESTADO_VENTA>();

    public DbSet<PB_ESTADO_PEDIDO> EstadosPedido =>
        Set<PB_ESTADO_PEDIDO>();

    public DbSet<PB_PEDIDO_CLIENTE> PedidosCliente =>
        Set<PB_PEDIDO_CLIENTE>();

    public DbSet<PB_PEDIDO_CLIENTE_DETALLE> PedidosClienteDetalles =>
        Set<PB_PEDIDO_CLIENTE_DETALLE>();

    public DbSet<PB_VENTA> Ventas =>
        Set<PB_VENTA>();

    public DbSet<PB_VENTA_DETALLE> VentasDetalles =>
        Set<PB_VENTA_DETALLE>();

    public DbSet<PB_VENTA_PAGO> VentasPagos =>
        Set<PB_VENTA_PAGO>();

    public DbSet<PB_ESTADO_DEVOLUCION> EstadosDevolucion =>
        Set<PB_ESTADO_DEVOLUCION>();

    public DbSet<PB_MOTIVO_DEVOLUCION> MotivosDevolucion =>
        Set<PB_MOTIVO_DEVOLUCION>();

    public DbSet<PB_DEVOLUCION_VENTA> DevolucionesVenta =>
        Set<PB_DEVOLUCION_VENTA>();

    public DbSet<PB_DEVOLUCION_VENTA_DETALLE>
        DevolucionesVentaDetalles =>
        Set<PB_DEVOLUCION_VENTA_DETALLE>();

    public DbSet<PB_DEVOLUCION_PAGO> DevolucionesPagos =>
        Set<PB_DEVOLUCION_PAGO>();

    // Cash

    public DbSet<PB_CAJA> Cajas =>
        Set<PB_CAJA>();

    public DbSet<PB_APERTURA_CAJA> AperturasCaja =>
        Set<PB_APERTURA_CAJA>();

    public DbSet<PB_CIERRE_CAJA> CierresCaja =>
        Set<PB_CIERRE_CAJA>();

    public DbSet<PB_CIERRE_CAJA_DETALLE> CierresCajaDetalles =>
        Set<PB_CIERRE_CAJA_DETALLE>();

    public DbSet<PB_TIPO_MOVIMIENTO_CAJA> TiposMovimientoCaja =>
        Set<PB_TIPO_MOVIMIENTO_CAJA>();

    public DbSet<PB_MOVIMIENTO_CAJA> MovimientosCaja =>
        Set<PB_MOVIMIENTO_CAJA>();

    // Audit

    public DbSet<PB_AUDITORIA> Auditorias =>
        Set<PB_AUDITORIA>();

    public DbSet<PB_AUDITORIA_DETALLE> AuditoriasDetalles =>
        Set<PB_AUDITORIA_DETALLE>();

    public DbSet<PB_HISTORIAL_ACCESO> HistorialAccesos =>
        Set<PB_HISTORIAL_ACCESO>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureGeography();
        builder.ConfigurePeople();
        builder.ConfigureAdministration();
        builder.ConfigureCatalog();
        builder.ConfigurePayments();
        builder.ConfigureSystem();
        builder.ConfigurePurchasing();
        builder.ConfigureInventory();
        builder.ConfigureSales();
        builder.ConfigureCash();
        builder.ConfigureAudit();
        builder.ConfigureIdentity();
    }
}