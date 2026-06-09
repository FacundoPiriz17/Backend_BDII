namespace Backend_BDII.Modules.Transferencias.DTOs;

public sealed class CrearTransferenciaRequest
{
    public int IdEntrada { get; init; }
    public required string EmailDestino { get; init; }
}
