using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Sales;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Seed;

namespace TikalLibros.Infrastructure.Services.Sales;

/// <summary>
/// Factura electrónica de las ventas con certificación simulada: genera un
/// número de autorización (UUID) y, como en FEL, la serie son sus primeros 8
/// caracteres hexadecimales y el número los 8 siguientes como entero. Guarda
/// una copia del receptor y de los totales tal como se emitieron.
/// </summary>
public sealed class InvoiceService
{
    public const string FinalConsumerNit = "CF";
    public const string FinalConsumerName = "Consumidor final";
    private const string NitCode = "NIT";

    private readonly AppDbContext _context;
    private readonly ISystemParameterService _parameters;

    public InvoiceService(AppDbContext context, ISystemParameterService parameters)
    {
        _context = context;
        _parameters = parameters;
    }

    /// <summary>Agrega la factura de la venta (sin guardar); la venta ya debe tener id.</summary>
    internal async Task<PB_FACTURA_VENTA> IssueAsync(PB_VENTA sale, CancellationToken cancellationToken)
    {
        (string nit, string name) = await ReceiverAsync(sale.VEN_ClienteId, cancellationToken);
        Guid authorization = Guid.NewGuid();

        PB_FACTURA_VENTA invoice = new()
        {
            FAC_VentaId = sale.VEN_Venta,
            FAC_NumeroAutorizacion = authorization,
            FAC_Serie = SeriesOf(authorization),
            FAC_Numero = NumberOf(authorization),
            FAC_FechaEmision = sale.VEN_Fecha,
            FAC_FechaCertificacion = DateTime.UtcNow,
            FAC_NitEmisor = await _parameters.GetStringAsync(SystemParameterKeys.InvoiceIssuerNit, cancellationToken),
            FAC_NitReceptor = nit,
            FAC_NombreReceptor = name,
            FAC_Total = sale.VEN_Total,
            FAC_Impuesto = sale.VEN_Impuesto,
            FAC_EsDemostracion = true,
            FAC_FechaCreacion = DateTime.UtcNow
        };

        _context.FacturasVenta.Add(invoice);

        return invoice;
    }

    internal static void Annul(PB_FACTURA_VENTA invoice, string reason, DateTime when)
    {
        invoice.FAC_Anulada = true;
        invoice.FAC_FechaAnulacion = when;
        invoice.FAC_MotivoAnulacion = reason.Length <= 500 ? reason : reason[..500];
    }

    /// <summary>
    /// Emite la factura de las ventas registradas antes de la facturación
    /// (las anuladas quedan con la factura anulada). Devuelve cuántas emitió.
    /// </summary>
    public async Task<int> IssueMissingAsync(CancellationToken cancellationToken = default)
    {
        List<PB_VENTA> sales = await _context.Ventas
            .Where(sale => sale.Factura == null)
            .OrderBy(sale => sale.VEN_Venta)
            .ToListAsync(cancellationToken);

        foreach (PB_VENTA sale in sales)
        {
            PB_FACTURA_VENTA invoice = await IssueAsync(sale, cancellationToken);

            if (sale.VEN_Anulada)
            {
                Annul(invoice, sale.VEN_MotivoAnulacion ?? "Venta anulada.", sale.VEN_FechaAnulacion ?? DateTime.UtcNow);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return sales.Count;
    }

    public async Task<InvoiceResponse?> GetAsync(int saleId, CancellationToken cancellationToken = default)
    {
        InvoiceResponse? response = await _context.FacturasVenta
            .AsNoTracking()
            .Where(invoice => invoice.FAC_VentaId == saleId)
            .Select(invoice => new InvoiceResponse
            {
                SaleId = invoice.FAC_VentaId,
                SaleNumber = invoice.Venta.VEN_Numero,
                IssuerAddress = invoice.Venta.Sucursal.Direccion.DIR_Linea1 +
                                (invoice.Venta.Sucursal.Direccion.DIR_Zona != null ? ", zona " + invoice.Venta.Sucursal.Direccion.DIR_Zona : "") +
                                ", " + invoice.Venta.Sucursal.Direccion.Municipio.MUN_Nombre +
                                ", " + invoice.Venta.Sucursal.Direccion.Municipio.Departamento.DEP_Nombre,
                IssuerNit = invoice.FAC_NitEmisor,
                Series = invoice.FAC_Serie,
                Number = invoice.FAC_Numero,
                IssuedAt = invoice.FAC_FechaEmision,
                CertifiedAt = invoice.FAC_FechaCertificacion,
                IsDemo = invoice.FAC_EsDemostracion,
                ReceiverNit = invoice.FAC_NitReceptor,
                ReceiverName = invoice.FAC_NombreReceptor,
                SaleType = invoice.Venta.TipoVenta.TVE_Nombre,
                CreditDueDate = invoice.Venta.VEN_FechaVencimientoCredito,
                Seller = invoice.Venta.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                         invoice.Venta.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                Subtotal = invoice.Venta.VEN_Subtotal,
                Discount = invoice.Venta.VEN_Descuento,
                Tax = invoice.FAC_Impuesto,
                Total = invoice.FAC_Total,
                Annulled = invoice.FAC_Anulada,
                AnnulledAt = invoice.FAC_FechaAnulacion,
                AnnulmentReason = invoice.FAC_MotivoAnulacion,
                Lines = invoice.Venta.Detalles
                    .OrderBy(line => line.VDE_NumeroLinea)
                    .Select(line => new InvoiceLineResponse
                    {
                        Quantity = line.VDE_Cantidad,
                        Description = line.Libro.LIB_Titulo + " (" + line.Libro.LIB_Codigo + ")",
                        UnitPrice = line.VDE_PrecioUnitario,
                        Discount = line.VDE_MontoDescuento,
                        Total = line.VDE_Total
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return null;
        }

        Guid authorization = await _context.FacturasVenta
            .Where(invoice => invoice.FAC_VentaId == saleId)
            .Select(invoice => invoice.FAC_NumeroAutorizacion)
            .SingleAsync(cancellationToken);

        var issuer = await _context.Empresas
            .AsNoTracking()
            .Where(company => company.EMP_Codigo == OrganizationSeeder.CompanyCode)
            .Select(company => new
            {
                company.EMP_NombreComercial,
                LegalName = company.Persona.PersonaJuridica != null ? company.Persona.PersonaJuridica.PJU_RazonSocial : company.EMP_NombreComercial
            })
            .FirstAsync(cancellationToken);

        response.Authorization = authorization.ToString("D").ToUpperInvariant();
        response.IssuerName = issuer.LegalName;
        response.IssuerTradeName = issuer.EMP_NombreComercial;
        response.TotalInWords = AmountInWords.Quetzales(response.Total);

        return response;
    }

    /// <summary>NIT y nombre del receptor; sin cliente o sin NIT, consumidor final.</summary>
    private async Task<(string Nit, string Name)> ReceiverAsync(int? customerId, CancellationToken cancellationToken)
    {
        if (customerId is null)
        {
            return (FinalConsumerNit, FinalConsumerName);
        }

        var customer = await _context.Clientes
            .Where(item => item.CLI_Cliente == customerId)
            .Select(item => new
            {
                Name = item.Persona.PersonaJuridica != null
                    ? item.Persona.PersonaJuridica.PJU_RazonSocial
                    : item.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      (item.Persona.PersonaNatural.PNA_SegundoNombre != null ? item.Persona.PersonaNatural.PNA_SegundoNombre + " " : "") +
                      item.Persona.PersonaNatural.PNA_PrimerApellido +
                      (item.Persona.PersonaNatural.PNA_SegundoApellido != null ? " " + item.Persona.PersonaNatural.PNA_SegundoApellido : ""),
                Nit = item.Persona.Identificaciones
                    .Where(identification => identification.PID_Activo && identification.TipoIdentificacion.TID_Codigo == NitCode)
                    .Select(identification => identification.PID_Numero)
                    .FirstOrDefault()
            })
            .FirstAsync(cancellationToken);

        return (string.IsNullOrWhiteSpace(customer.Nit) ? FinalConsumerNit : customer.Nit, customer.Name);
    }

    internal static string SeriesOf(Guid authorization) =>
        authorization.ToString("N")[..8].ToUpperInvariant();

    internal static long NumberOf(Guid authorization) =>
        Convert.ToInt64(authorization.ToString("N")[8..16], 16);
}
