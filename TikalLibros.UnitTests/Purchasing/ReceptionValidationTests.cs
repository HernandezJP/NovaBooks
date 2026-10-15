using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Purchasing;

namespace TikalLibros.UnitTests.Purchasing;

public sealed class ReceptionValidationTests
{
    private static List<string> Members(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).ToList();
    }

    private static RegisterReceptionRequest Valid() => new()
    {
        PurchaseOrderId = 1,
        Lines = [new ReceptionLineRequest { PurchaseOrderLineId = 1, Accepted = 5 }]
    };

    [Fact]
    public void Recepcion_Valida_NoTieneErrores()
    {
        Assert.Empty(Members(Valid()));
    }

    [Fact]
    public void Recepcion_LineaSinCantidadesYRechazoSinMotivo_TieneErrores()
    {
        RegisterReceptionRequest request = Valid();
        request.Lines =
        [
            new ReceptionLineRequest { PurchaseOrderLineId = 1 },
            new ReceptionLineRequest { PurchaseOrderLineId = 2, Accepted = 1, Rejected = 2, RejectReason = "mal" }
        ];

        List<string> members = Members(request);

        Assert.Contains("Lines[0].Accepted", members);
        Assert.Contains("Lines[1].RejectReason", members);
    }

    [Theory]
    [InlineData("A1B2-C3", "123456", true)]
    [InlineData("A1 B2", "123456", false)]
    [InlineData("SERIE", "12-34", false)]
    [InlineData("SERIE", "1234A", false)]
    public void Recepcion_SerieYNumeroDeFactura(string series, string number, bool valid)
    {
        RegisterReceptionRequest request = Valid();
        request.SupplierDocumentSeries = series;
        request.SupplierDocumentNumber = number;

        Assert.Equal(valid, Members(request).Count == 0);
    }

    [Fact]
    public void Recepcion_SerieSinNumero_TieneError()
    {
        RegisterReceptionRequest request = Valid();
        request.SupplierDocumentSeries = "A1";

        Assert.Contains(nameof(RegisterReceptionRequest.SupplierDocumentNumber), Members(request));
    }

    [Fact]
    public void Recepcion_FechaFutura_TieneError()
    {
        RegisterReceptionRequest request = Valid();
        request.ReceivedOn = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        Assert.Contains(nameof(RegisterReceptionRequest.ReceivedOn), Members(request));
    }

    [Theory]
    [InlineData("Libros dañados: 2 (caja #3), revisar.", true)]
    [InlineData("Línea 1\nLínea 2", true)]
    [InlineData("<script>alert(1)</script>", false)]
    public void Anulacion_MotivoSoloTextoPlano(string reason, bool valid)
    {
        Assert.Equal(valid, Members(new AnnulReceptionRequest { Reason = reason }).Count == 0);
    }
}
