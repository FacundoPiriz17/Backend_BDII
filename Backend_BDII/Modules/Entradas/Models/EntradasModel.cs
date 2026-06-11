namespace Backend_BDII.Modules.Entradas.Models;

public class NuevaEntrada
{
    public required int idEntrada { get; init; }
    
    public required DateTime fechaHora { get; init; }
    
    public required string estado { get; init; }

    public required string codigoQr { get; init; }
    
    public required int costoTotal { get; init; }
    
    public required int transferenciasRestantes { get; init; }
    
    public required int idCompra { get; init; }
    
    public required int idPartido { get; init; }
    
    public required string nombreSector { get; init; }
    
    public required int idEstadio { get; init; }
    
    public required string emailPropietarioActual { get; init; }

}