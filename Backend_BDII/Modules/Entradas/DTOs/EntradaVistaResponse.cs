using Backend_BDII.Modules.Compras.DTOs;

namespace Backend_BDII.Modules.Entradas.DTOs;

public sealed class EntradaVistaResponse
{
    public required EntradaDetalleResponse Entrada { get; init; }
    public required QrEntradaResponse Qr { get; init; }
    public int RefrescarCadaSegundos { get; init; } = 30;
}
