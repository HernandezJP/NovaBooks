using System.Net;
using NovaBooks.IntegrationTests.Infrastructure;

namespace NovaBooks.IntegrationTests.Api;

public sealed class ErrorHandlingTests : ApiTestBase
{
    public ErrorHandlingTests(NovaBooksApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RecursoInexistente_Devuelve404ProblemDetailsSinDetallesInternos()
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.GetAsync("api/clientes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("   at ", body);
        Assert.DoesNotContain("Exception", body);
    }

    [Fact]
    public async Task RutaDesconocida_Devuelve404()
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.GetAsync("api/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>Mientras la base se inicializa la API responde 503.</summary>
public sealed class DatabaseStartingTests : IClassFixture<NotReadyApiFactory>
{
    private readonly NotReadyApiFactory _factory;

    public DatabaseStartingTests(NotReadyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BaseNoLista_Devuelve503ConCodigoSystemStarting()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("api/auth/me");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.Contains("Retry-After"));
        Assert.Contains("system_starting", await response.Content.ReadAsStringAsync());
    }
}
