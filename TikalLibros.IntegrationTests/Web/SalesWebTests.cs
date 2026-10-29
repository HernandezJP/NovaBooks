extern alias web;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using web::TikalLibros.Web.Components.Pages.Sales;
using web::TikalLibros.Web.Components.Shared;
using web::TikalLibros.Web.Components.Shared.Sales;
using web::TikalLibros.Web.Services.Api;
using web::TikalLibros.Web.Services.Reports;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Cálculos y validaciones de la pantalla de venta (los mismos que la API).</summary>
public sealed class SaleRowsTests
{
    private static readonly PaymentMethodResponse Cash = new() { Id = 1, Code = PaymentMethodCode.Cash, Name = "Efectivo", AllowsChange = true };
    private static readonly PaymentMethodResponse Card = new() { Id = 2, Code = PaymentMethodCode.Card, Name = "Tarjeta", RequiresReference = true, RequiresAuthorization = true };
    private static readonly PaymentMethodResponse Transfer = new() { Id = 3, Code = PaymentMethodCode.Transfer, Name = "Transferencia", RequiresReference = true };

    [Fact]
    public void Linea_CalculaIgualQueLaApi()
    {
        SaleLineRow line = new() { BookId = 1, UnitPrice = 112m, TaxPercent = 12m, Quantity = 2, DiscountPercent = 5m };

        Assert.Equal(new SaleAmounts(224m, 11.20m, 22.80m, 212.80m), line.Amounts);
        Assert.Null(line.Error(maximumDiscount: 50m));
        Assert.NotNull(new SaleLineRow { Quantity = 0 }.Error(50m));
        Assert.NotNull(new SaleLineRow { Quantity = 1, DiscountPercent = 60m }.Error(50m));
    }

    [Fact]
    public void Efectivo_CalculaElCambioYNoAdmiteRecibirMenos()
    {
        PaymentRow payment = new() { Method = Cash, Amount = 212.80m, Received = 250m };

        Assert.Equal(37.20m, payment.Change);
        Assert.Null(payment.Error);
        Assert.NotNull(new PaymentRow { Method = Cash, Amount = 100m, Received = 50m }.Error);
        Assert.Null(new PaymentRow { Method = Card, Amount = 10m, Received = 20m }.ToRequest().Received);
    }

    [Fact]
    public void TarjetaYTransferencia_ExigenSusDatos()
    {
        Assert.Contains("4 dígitos", new PaymentRow { Method = Card, Amount = 10m, Authorization = "A1" }.Error);
        Assert.Contains("autorización", new PaymentRow { Method = Card, Amount = 10m, LastFourDigits = "4242" }.Error);
        Assert.Null(new PaymentRow { Method = Card, Amount = 10m, LastFourDigits = "4242", Authorization = "A1" }.Error);
        Assert.Contains("referencia", new PaymentRow { Method = Transfer, Amount = 10m }.Error);
    }

    [Fact]
    public void Pagos_AlContadoDebenCubrirElTotalYNuncaExcederlo()
    {
        Assert.Contains("Faltan Q 12.80", PaymentRules.Error([new PaymentRow { Method = Cash, Amount = 200m }], 212.80m, requireFull: true));
        Assert.Null(PaymentRules.Error([new PaymentRow { Method = Cash, Amount = 200m }], 212.80m, requireFull: false));
        Assert.Contains("superan", PaymentRules.Error([new PaymentRow { Method = Card, Amount = 300m, LastFourDigits = "1111", Authorization = "A" }], 212.80m, true));
        Assert.Null(PaymentRules.Error(
            [new PaymentRow { Method = Cash, Amount = 112.80m, Received = 120m }, new PaymentRow { Method = Card, Amount = 100m, LastFourDigits = "1111", Authorization = "A" }],
            212.80m, true));
    }
}

public sealed class CsvExportTests
{
    [Fact]
    public void Csv_EscapaComasComillasYFormulas()
    {
        string csv = CsvExport.Build(
            ["Título", "Total"],
            [["Cien años, \"edición\"", 1234.5m], ["=SUMA(A1)", 10m]]);

        string[] lines = csv.TrimEnd().Split(Environment.NewLine);
        Assert.Equal("Título,Total", lines[0]);
        Assert.Equal("\"Cien años, \"\"edición\"\"\",1234.5", lines[1]);
        Assert.Equal("'=SUMA(A1),10", lines[2]);
    }
}

/// <summary>URLs que arman los clientes de pedidos, ventas, reportes y facturas.</summary>
public sealed class SalesApiTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task Ventas_EnviaLaBusquedaAvanzada()
    {
        Handler.Respond = _ => Json(new PagedResponse<SaleListItemResponse>());

        await new SalesApi(Api).GetPagedAsync(new SaleQuery
        {
            Search = " CF ",
            CustomerId = 4,
            PaymentMethodId = 2,
            WithBalance = true,
            From = new DateOnly(2026, 10, 1),
            Page = 2,
            PageSize = 15
        });

        Assert.Equal("/api/ventas?search=CF&customerId=4&paymentMethodId=2&withBalance=true&from=2026-10-01&page=2&pageSize=15", Handler.LastPathAndQuery);
    }

    [Fact]
    public async Task Pedidos_EnviaEstadoYMontos()
    {
        Handler.Respond = _ => Json(new PagedResponse<OrderListItemResponse>());

        await new OrdersApi(Api).GetPagedAsync(new OrderQuery { Status = OrderStatus.PendingApproval, MinTotal = 100m, MaxTotal = 250.5m });

        Assert.Equal("/api/pedidos?status=POR_APROBAR&minTotal=100&maxTotal=250.5&page=1&pageSize=10", Handler.LastPathAndQuery);
    }

    [Fact]
    public async Task Reportes_YFactura_UsanSusRutas()
    {
        Handler.Respond = _ => Json(new TopBooksResponse());
        await new ReportsApi(Api).GetTopBooksAsync(new TopBooksQuery { From = new DateOnly(2026, 10, 1), To = new DateOnly(2026, 10, 21), Top = 20, OrderBy = "revenue" });
        Assert.Equal("/api/reportes/libros-mas-vendidos?from=2026-10-01&to=2026-10-21&top=20&orderBy=revenue", Handler.LastPathAndQuery);

        Handler.Respond = _ => Json(new InvoiceResponse());
        await new SalesApi(Api).GetInvoiceAsync(7);
        Assert.Equal("/api/ventas/7/factura", Handler.LastPath);
    }
}

public sealed class SalesComponentTests : WebTestContext
{
    private static readonly PaymentMethodResponse[] Methods =
    [
        new() { Id = 1, Code = PaymentMethodCode.Cash, Name = "Efectivo", AllowsChange = true },
        new() { Id = 2, Code = PaymentMethodCode.Card, Name = "Tarjeta", RequiresReference = true, RequiresAuthorization = true }
    ];

    [Fact]
    public void EditorDePagos_MuestraLoQueFaltaYElCambio()
    {
        List<PaymentRow> payments = [new PaymentRow { Method = Methods[0], Amount = 100m, Received = 120m }];

        IRenderedComponent<PaymentsEditor> editor = Render<PaymentsEditor>(parameters => parameters
            .Add(p => p.Methods, Methods)
            .Add(p => p.Payments, payments)
            .Add(p => p.Due, 150m)
            .Add(p => p.Submitted, true));

        Assert.Contains("Falta", editor.Markup);
        Assert.Contains("50.00", editor.Markup);
        Assert.Contains("Cambio", editor.Markup);
        Assert.Contains("Faltan Q 50.00 por cobrar.", editor.Markup);
    }

    [Fact]
    public void EditorDePagos_AlCreditoLoPendienteQuedaAlCredito()
    {
        IRenderedComponent<PaymentsEditor> editor = Render<PaymentsEditor>(parameters => parameters
            .Add(p => p.Methods, Methods)
            .Add(p => p.Payments, new List<PaymentRow>())
            .Add(p => p.Due, 80m)
            .Add(p => p.RequireFull, false)
            .Add(p => p.Submitted, true));

        Assert.Contains("Queda al crédito", editor.Markup);
        Assert.DoesNotContain("por cobrar", editor.Markup);
    }

    [Fact]
    public void FacturaImprimible_MuestraDatosFelTotalEnLetrasYLeyenda()
    {
        Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new InvoiceResponse
            {
                SaleId = 7,
                SaleNumber = "VEN-000007",
                IssuerName = "Tikal Libros, S.A.",
                IssuerTradeName = "Tikal Libros",
                IssuerNit = "4567890K",
                Series = "B33C2E9A",
                Number = 3662825818,
                Authorization = "B33C2E9A-DA51-4F3A-9C1E-0A1B2C3D4E5F",
                IsDemo = true,
                ReceiverNit = "CF",
                ReceiverName = "Consumidor final",
                SaleType = "Contado",
                Lines = [new InvoiceLineResponse { Quantity = 2, Description = "Rayuela (LIB-000001)", UnitPrice = 112m, Discount = 11.20m, Total = 212.80m }],
                Subtotal = 224m,
                Discount = 11.20m,
                Tax = 22.80m,
                Total = 212.80m,
                TotalInWords = "Doscientos doce quetzales con 80/100",
                Annulled = true,
                AnnulmentReason = "Venta de prueba"
            })
        };
        Render<MudBlazor.MudPopoverProvider>();

        IRenderedComponent<InvoicePrint> page = Render<InvoicePrint>(parameters => parameters.Add(p => p.Id, 7));

        page.WaitForAssertion(() => Assert.Contains("B33C2E9A", page.Markup));
        Assert.Equal("/api/ventas/7/factura", Handler.LastPath);
        Assert.Contains("3662825818", page.Markup);
        Assert.Contains("Doscientos doce quetzales con 80/100", page.Markup);
        Assert.Contains("Sin validez fiscal", page.Markup);
        Assert.Contains("Consumidor final", page.Markup);
        Assert.Contains("invoice--annulled", page.Find("article").ClassName);
    }

    [Theory]
    [InlineData(OrderStatus.PendingApproval, "status-pill--warning")]
    [InlineData(OrderStatus.Reserved, "status-pill--info")]
    [InlineData(OrderStatus.Invoiced, "status-pill--active")]
    [InlineData(OrderStatus.Rejected, "status-pill--danger")]
    [InlineData(SaleStatus.Paid, "status-pill--active")]
    [InlineData(SaleStatus.Credit, "status-pill--info")]
    public void Estado_DePedidoYVenta_UsaSuColor(string code, string css)
    {
        IRenderedComponent<DocumentStatus> status = Render<DocumentStatus>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Text, code));

        Assert.Contains(css, status.Find("span").ClassName);
    }
}
