using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Domain.Entities.Audit;
using TikalLibros.Domain.Entities.Cash;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Caja: apertura, movimientos manuales, anulación y cierre con arqueo.</summary>
public sealed class CashTests : ApiTestBase
{
    private static readonly string[] Cashier = [CashPermissions.View, CashPermissions.Open, CashPermissions.Close];

    private static readonly string[] CashierWithMovements =
        [.. Cashier, CashPermissions.CreateIncome, CashPermissions.CreateExpense];

    public CashTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    /// <summary>Caja propia de la prueba: solo puede haber una apertura por caja.</summary>
    private Task<int> CreateRegisterAsync() =>
        Factory.QueryAsync(async context =>
        {
            int branch = await context.Sucursales
                .Where(item => item.SUC_Codigo == OrganizationSeeder.MainBranchCode)
                .Select(item => item.SUC_Sucursal)
                .SingleAsync();

            PB_CAJA register = new()
            {
                CAJ_SucursalId = branch,
                CAJ_Codigo = Unique("CJ").ToUpperInvariant(),
                CAJ_Nombre = Unique("Caja de prueba "),
                CAJ_Activo = true
            };

            context.Cajas.Add(register);
            await context.SaveChangesAsync();

            return register.CAJ_Caja;
        });

    private static async Task<T> OkAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<CashSessionResponse> OpenAsync(HttpClient client, int register, decimal amount = 200m) =>
        await OkAsync<CashSessionResponse>(
            await client.PostAsJsonAsync("api/caja/aperturas", new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = amount }),
            HttpStatusCode.Created);

    private static Task<HttpResponseMessage> MovementAsync(HttpClient client, int session, string type, decimal amount) =>
        client.PostAsJsonAsync($"api/caja/aperturas/{session}/movimientos", new CashMovementRequest
        {
            TypeCode = type,
            Amount = amount,
            Concept = "Movimiento de prueba de caja"
        });

    private static Task<HttpResponseMessage> CloseAsync(HttpClient client, int session, decimal counted, string? notes = null) =>
        client.PostAsJsonAsync($"api/caja/aperturas/{session}/cerrar", new CloseCashSessionRequest { CountedCash = counted, Notes = notes });

    [Fact]
    public async Task Abrir_CreaLaAperturaYLaCajaApareceAbierta()
    {
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        int register = await CreateRegisterAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await cashier.GetAsync("api/caja/aperturas/actual")).StatusCode);

        CashSessionResponse session = await OpenAsync(cashier, register, 250m);

        Assert.True(session.IsOpen && session.IsMine);
        Assert.Equal(250m, session.ExpectedCash);

        CashSessionResponse current = await OkAsync<CashSessionResponse>(await cashier.GetAsync("api/caja/aperturas/actual"));
        Assert.Equal(session.Id, current.Id);

        CashCatalogsResponse catalogs = (await cashier.GetFromJsonAsync<CashCatalogsResponse>("api/caja/catalogos"))!;
        CashRegisterResponse listed = Assert.Single(catalogs.CashRegisters, item => item.Id == register);
        Assert.Equal(session.Id, listed.OpenSessionId);
        Assert.DoesNotContain(catalogs.MovementTypes, type => type.Code == CashMovementTypes.Sale);
    }

    [Fact]
    public async Task Abrir_CajaYaAbiertaOEmpleadoConOtraCaja_Devuelve409()
    {
        (HttpClient first, _) = await EmployeeClientAsync(Cashier);
        (HttpClient second, _) = await EmployeeClientAsync(Cashier);
        int register = await CreateRegisterAsync();
        int other = await CreateRegisterAsync();
        await OpenAsync(first, register);

        HttpResponseMessage sameRegister = await second.PostAsJsonAsync("api/caja/aperturas",
            new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = 0m });
        HttpResponseMessage secondRegister = await first.PostAsJsonAsync("api/caja/aperturas",
            new OpenCashSessionRequest { CashRegisterId = other, InitialAmount = 0m });

        Assert.Equal(HttpStatusCode.Conflict, sameRegister.StatusCode);
        Assert.Contains("ya está abierta por", await ReadProblemDetailAsync(sameRegister));
        Assert.Equal(HttpStatusCode.Conflict, secondRegister.StatusCode);
        Assert.Contains("Ya tiene abierta la caja", await ReadProblemDetailAsync(secondRegister));
    }

    [Fact]
    public async Task Abrir_SinEmpleadoOConMontoInvalido_SeRechaza()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync(Cashier);
        HttpClient withoutEmployee = await LoginAsync(userName, TestPassword);
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        int register = await CreateRegisterAsync();

        Assert.Equal(HttpStatusCode.Conflict, (await withoutEmployee.PostAsJsonAsync("api/caja/aperturas",
            new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = 0m })).StatusCode);

        HttpResponseMessage invalid = await cashier.PostAsJsonAsync("api/caja/aperturas",
            new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = -1m });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.True((await ReadJsonAsync(invalid)).GetProperty("errors").TryGetProperty("InitialAmount", out _));

        Assert.Equal(HttpStatusCode.BadRequest, (await cashier.PostAsJsonAsync("api/caja/aperturas",
            new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = 10.123m })).StatusCode);
    }

    [Fact]
    public async Task Movimientos_IngresosYEgresosActualizanElEfectivoEsperado()
    {
        (HttpClient cashier, _) = await EmployeeClientAsync(CashierWithMovements);
        CashSessionResponse session = await OpenAsync(cashier, await CreateRegisterAsync(), 200m);

        await OkAsync<CashSessionResponse>(await MovementAsync(cashier, session.Id, CashMovementTypes.ManualIncome, 50m));
        CashSessionResponse updated = await OkAsync<CashSessionResponse>(await MovementAsync(cashier, session.Id, CashMovementTypes.Expense, 30m));

        Assert.Equal(50m, updated.CashIncome);
        Assert.Equal(30m, updated.CashExpense);
        Assert.Equal(220m, updated.ExpectedCash);
        Assert.All(updated.Movements, movement => Assert.Matches(@"^MCJ-\d{6}$", movement.Number));

        HttpResponseMessage tooMuch = await MovementAsync(cashier, session.Id, CashMovementTypes.Withdrawal, 220.01m);
        Assert.Equal(HttpStatusCode.Conflict, tooMuch.StatusCode);
        Assert.Contains("No hay suficiente efectivo", await ReadProblemDetailAsync(tooMuch));

        HttpResponseMessage sale = await MovementAsync(cashier, session.Id, CashMovementTypes.Sale, 10m);
        Assert.Equal(HttpStatusCode.BadRequest, sale.StatusCode);
        Assert.Contains("los registra el sistema", await ReadProblemDetailAsync(sale));
    }

    [Fact]
    public async Task Movimientos_SinPermisoDeEgresoOEnCajaAjena_Devuelve403()
    {
        (HttpClient incomeOnly, _) = await EmployeeClientAsync([.. Cashier, CashPermissions.CreateIncome]);
        (HttpClient other, _) = await EmployeeClientAsync(CashierWithMovements);
        CashSessionResponse session = await OpenAsync(incomeOnly, await CreateRegisterAsync());

        Assert.Equal(HttpStatusCode.Forbidden, (await MovementAsync(incomeOnly, session.Id, CashMovementTypes.Expense, 5m)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MovementAsync(other, session.Id, CashMovementTypes.ManualIncome, 5m)).StatusCode);

        (HttpClient noMovements, _) = await EmployeeClientAsync(Cashier);
        Assert.Equal(HttpStatusCode.Forbidden, (await MovementAsync(noMovements, session.Id, CashMovementTypes.ManualIncome, 5m)).StatusCode);
    }

    [Fact]
    public async Task Cerrar_Cuadrado_CierraYPermiteAbrirDeNuevo()
    {
        (HttpClient cashier, _) = await EmployeeClientAsync(CashierWithMovements);
        int register = await CreateRegisterAsync();
        CashSessionResponse session = await OpenAsync(cashier, register, 100m);
        await MovementAsync(cashier, session.Id, CashMovementTypes.ManualIncome, 25.50m);

        CashSessionResponse closed = await OkAsync<CashSessionResponse>(await CloseAsync(cashier, session.Id, 125.50m));

        Assert.False(closed.IsOpen);
        Assert.NotNull(closed.Closing);
        Assert.Equal(125.50m, closed.Closing!.ExpectedCash);
        Assert.Equal(0m, closed.Closing.Difference);
        Assert.Contains(closed.Closing.Lines, line => line.PaymentMethod == "Efectivo" && line.System == 125.50m);

        Assert.Equal(HttpStatusCode.Conflict, (await MovementAsync(cashier, session.Id, CashMovementTypes.ManualIncome, 5m)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await CloseAsync(cashier, session.Id, 125.50m)).StatusCode);
        await OpenAsync(cashier, register, 50m);
    }

    [Fact]
    public async Task Cerrar_ConDiferencia_ExigeObservacionYLaRegistra()
    {
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        CashSessionResponse session = await OpenAsync(cashier, await CreateRegisterAsync(), 100m);

        HttpResponseMessage withoutNotes = await CloseAsync(cashier, session.Id, 95m);
        Assert.Equal(HttpStatusCode.BadRequest, withoutNotes.StatusCode);
        Assert.Contains("no cuadra", await ReadProblemDetailAsync(withoutNotes));

        CashSessionResponse closed = await OkAsync<CashSessionResponse>(
            await CloseAsync(cashier, session.Id, 95m, "Faltan cinco quetzales del cambio entregado."));

        Assert.Equal(-5m, closed.Closing!.Difference);
        Assert.Equal("Faltan cinco quetzales del cambio entregado.", closed.Closing.Notes);
    }

    [Fact]
    public async Task Cerrar_CajaAjena_SoloConPermisoDeSupervision()
    {
        (HttpClient owner, _) = await EmployeeClientAsync(Cashier);
        (HttpClient otherCashier, _) = await EmployeeClientAsync(Cashier);
        (HttpClient supervisor, _) = await EmployeeClientAsync([.. Cashier, CashPermissions.CancelMovement]);
        CashSessionResponse session = await OpenAsync(owner, await CreateRegisterAsync(), 10m);

        Assert.Equal(HttpStatusCode.Forbidden, (await CloseAsync(otherCashier, session.Id, 10m)).StatusCode);
        Assert.False((await OkAsync<CashSessionResponse>(await CloseAsync(supervisor, session.Id, 10m))).IsOpen);
    }

    [Fact]
    public async Task AnularMovimiento_DejaDeContarYNoSeAnulaDosVeces()
    {
        (HttpClient supervisor, _) = await EmployeeClientAsync([.. CashierWithMovements, CashPermissions.CancelMovement]);
        CashSessionResponse session = await OpenAsync(supervisor, await CreateRegisterAsync(), 100m);
        CashSessionResponse withExpense = await OkAsync<CashSessionResponse>(
            await MovementAsync(supervisor, session.Id, CashMovementTypes.Expense, 40m));
        CashMovementResponse expense = Assert.Single(withExpense.Movements);

        AnnulCashMovementRequest request = new() { Reason = "Gasto registrado por error" };
        CashSessionResponse annulled = await OkAsync<CashSessionResponse>(
            await supervisor.PostAsJsonAsync($"api/caja/movimientos/{expense.Id}/anular", request));

        Assert.Equal(100m, annulled.ExpectedCash);
        Assert.True(Assert.Single(annulled.Movements).Annulled);
        Assert.Equal(HttpStatusCode.Conflict,
            (await supervisor.PostAsJsonAsync($"api/caja/movimientos/{expense.Id}/anular", request)).StatusCode);

        (HttpClient cashier, _) = await EmployeeClientAsync(CashierWithMovements);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.PostAsJsonAsync($"api/caja/movimientos/{expense.Id}/anular", request)).StatusCode);
    }

    [Theory]
    [InlineData("Cajero", CashPermissions.Open, true)]
    [InlineData("Cajero", CashPermissions.Close, true)]
    [InlineData("Cajero", CashPermissions.CreateIncome, true)]
    [InlineData("Cajero", CashPermissions.CreateExpense, false)]
    [InlineData("Cajero", CashPermissions.CancelMovement, false)]
    [InlineData("Supervisor", CashPermissions.CreateExpense, true)]
    [InlineData("Supervisor", CashPermissions.CancelMovement, true)]
    public async Task Roles_CajeroIngresaYSupervisorAutorizaEgresosYAnulaciones(string role, string permission, bool granted)
    {
        bool has = await Factory.QueryAsync(context => context.RoleClaims
            .Join(context.Roles, claim => claim.RoleId, item => item.Id, (claim, item) => new { item.Name, claim.ClaimType, claim.ClaimValue })
            .AnyAsync(item => item.Name == role && item.ClaimType == CustomClaimTypes.Permission && item.ClaimValue == permission));

        Assert.Equal(granted, has);
    }

    [Fact]
    public async Task AperturaYCierre_QuedanEnLaAuditoriaConSusMontos()
    {
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        CashSessionResponse session = await OpenAsync(cashier, await CreateRegisterAsync(), 80m);
        await CloseAsync(cashier, session.Id, 80m);

        List<PB_AUDITORIA> audit = await Factory.QueryAsync(context => context.Auditorias
            .Include(item => item.Detalles)
            .Where(item => item.AUD_Entidad == "AperturaCaja" && item.AUD_EntidadId == session.Id.ToString())
            .OrderBy(item => item.AUD_Auditoria)
            .ToListAsync());

        Assert.Equal(["ABRIR", "CERRAR"], audit.Select(item => item.AUD_Accion));
        Assert.Contains(audit[0].Detalles, change => change.ADE_Campo == "Monto inicial" && change.ADE_ValorNuevo == "80");
        Assert.Contains(audit[1].Detalles, change => change.ADE_Campo == "Efectivo contado" && change.ADE_ValorNuevo == "80");
    }
}
