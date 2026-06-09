namespace Backend_BDII.Common.Security;

public interface IEntradaQrCodeService
{
    string GeneratePayload(int? idEntrada, string ownerEmail);
    string GeneratePngBase64(string payload);
}
