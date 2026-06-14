using System.Security.Cryptography;
using QRCoder;

namespace Backend_BDII.Common.Security;
//Esta clase tiene la lógica de generación del QR, el primer método genera el código y el segundo la imagen del mismo
public sealed class EntradaQrCodeService : IEntradaQrCodeService
{
    public string GeneratePayload(int? idEntrada, string ownerEmail)
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var entradaPart = idEntrada?.ToString() ?? "new";

        return $"entrada:{entradaPart}|owner:{ownerEmail}|ts:{timestamp}|nonce:{nonce}";
    }

    public string GeneratePngBase64(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(20);

        return Convert.ToBase64String(bytes);
    }
}
