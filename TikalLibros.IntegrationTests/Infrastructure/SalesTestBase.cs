using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Customers;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Domain.Entities.Cash;
using TikalLibros.Infrastructure.Data.Seed;

namespace TikalLibros.IntegrationTests.Infrastructure;

/// <summary>Datos para pruebas de pedidos y ventas: clientes, libros con precio y existencia.</summary>
public abstract class SalesTestBase : ApiTestBase
{
    protected SalesTestBase(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    protected static async Task<T> OkAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    protected static async Task<int> CreateCustomerAsync(HttpClient admin, decimal creditLimit = 0m, int creditDays = 0)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("api/clientes", new SaveCustomerRequest
        {
            PersonType = SaveCustomerRequest.LegalPerson,
            LegalName = $"Colegio {UniqueName("Quetzal")}",
            Nit = RandomDigits(7) + "K",
            CreditLimit = creditLimit,
            CreditDays = creditDays
        });

        return (await OkAsync<CustomerResponse>(response, HttpStatusCode.Created)).Id;
    }

    /// <summary>Libro con precio (IVA incluido) en la lista general y, si se indica, existencia en el almacén principal.</summary>
    protected static async Task<int> CreateBookAsync(HttpClient admin, decimal price = 112m, int stock = 0)
    {
        ProductCatalogsResponse catalogs = (await admin.GetFromJsonAsync<ProductCatalogsResponse>("api/productos/catalogos"))!;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/productos", new SaveProductRequest
        {
            Title = Unique("Libro de venta "),
            Isbn13 = NewIsbn13(),
            LanguageId = catalogs.Languages.First().Id,
            FormatId = catalogs.Formats.First().Id,
            TaxId = catalogs.Taxes.First().Id,
            AllowsSale = true,
            Prices = [new ProductPriceRequest { PriceListId = catalogs.PriceLists.Single(list => list.IsDefault).Id, Price = price }]
        });

        int book = (await OkAsync<ProductResponse>(response, HttpStatusCode.Created)).Id;

        if (stock > 0)
        {
            await AddStockAsync(admin, book, stock);
        }

        return book;
    }

    /// <summary>Compra directa a un proveedor nuevo: entra existencia al almacén principal.</summary>
    protected static async Task AddStockAsync(HttpClient admin, int book, int quantity, decimal cost = 50m)
    {
        HttpResponseMessage supplier = await admin.PostAsJsonAsync("api/proveedores", new SaveSupplierRequest
        {
            PersonType = SaveSupplierRequest.LegalPerson,
            LegalName = $"Distribuidora {UniqueName("Atitlan")}",
            Nit = RandomDigits(8)
        });

        HttpResponseMessage purchase = await admin.PostAsJsonAsync("api/recepciones/compra-directa", new DirectPurchaseRequest
        {
            Order = new SavePurchaseOrderRequest
            {
                SupplierId = (await OkAsync<SupplierResponse>(supplier, HttpStatusCode.Created)).Id,
                Lines = [new PurchaseOrderLineRequest { BookId = book, Quantity = quantity, UnitCost = cost }]
            }
        });

        Assert.Equal(HttpStatusCode.Created, purchase.StatusCode);
    }

    protected static async Task<StockItemResponse> StockAsync(HttpClient admin, int book)
    {
        ProductResponse product = (await admin.GetFromJsonAsync<ProductResponse>($"api/productos/{book}"))!;
        PagedResponse<StockItemResponse> page = (await admin.GetFromJsonAsync<PagedResponse<StockItemResponse>>(
            $"api/inventario/existencias?search={product.Code}"))!;

        return Assert.Single(page.Items);
    }

    protected static SaveOrderRequest Order(int customer, params (int Book, int Quantity, decimal Discount)[] lines) => new()
    {
        CustomerId = customer,
        Lines = lines
            .Select(line => new OrderLineRequest { BookId = line.Book, Quantity = line.Quantity, DiscountPercent = line.Discount })
            .ToList()
    };

    protected static async Task<OrderResponse> CreateOrderAsync(HttpClient client, SaveOrderRequest request) =>
        await OkAsync<OrderResponse>(await client.PostAsJsonAsync("api/pedidos", request), HttpStatusCode.Created);

    protected static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, int order) =>
        client.PostAsync($"api/pedidos/{order}/confirmar", null);

    /// <summary>Abre una caja nueva para el usuario si no tiene una abierta.</summary>
    protected async Task<CashSessionResponse> EnsureCashAsync(HttpClient client)
    {
        HttpResponseMessage current = await client.GetAsync("api/caja/aperturas/actual");

        if (current.StatusCode == HttpStatusCode.OK)
        {
            return (await current.Content.ReadFromJsonAsync<CashSessionResponse>())!;
        }

        int register = await Factory.QueryAsync(async context =>
        {
            int branch = await context.Sucursales
                .Where(item => item.SUC_Codigo == OrganizationSeeder.MainBranchCode)
                .Select(item => item.SUC_Sucursal)
                .SingleAsync();

            PB_CAJA created = new()
            {
                CAJ_SucursalId = branch,
                CAJ_Codigo = Unique("CV").ToUpperInvariant(),
                CAJ_Nombre = Unique("Caja de ventas "),
                CAJ_Activo = true
            };

            context.Cajas.Add(created);
            await context.SaveChangesAsync();
            return created.CAJ_Caja;
        });

        return await OkAsync<CashSessionResponse>(
            await client.PostAsJsonAsync("api/caja/aperturas", new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = 100m }),
            HttpStatusCode.Created);
    }

    protected static async Task<int> MethodAsync(HttpClient client, string code) =>
        (await client.GetFromJsonAsync<SaleCatalogsResponse>("api/ventas/catalogos"))!.PaymentMethods.Single(method => method.Code == code).Id;

    protected static async Task<PaymentRequest> CashAsync(HttpClient client, decimal amount, decimal? received = null) =>
        new() { PaymentMethodId = await MethodAsync(client, PaymentMethodCode.Cash), Amount = amount, Received = received };

    protected static async Task<PaymentRequest> CardAsync(HttpClient client, decimal amount) =>
        new() { PaymentMethodId = await MethodAsync(client, PaymentMethodCode.Card), Amount = amount, Authorization = "A12345", LastFourDigits = "4242" };

    protected static DirectSaleRequest Sale(int? customer, IEnumerable<PaymentRequest> payments, params (int Book, int Quantity, decimal Discount)[] lines) => new()
    {
        CustomerId = customer,
        Lines = lines.Select(line => new OrderLineRequest { BookId = line.Book, Quantity = line.Quantity, DiscountPercent = line.Discount }).ToList(),
        Payments = payments.ToList()
    };

    protected static Task<HttpResponseMessage> SellAsync(HttpClient client, DirectSaleRequest request) =>
        client.PostAsJsonAsync("api/ventas", request);
}
