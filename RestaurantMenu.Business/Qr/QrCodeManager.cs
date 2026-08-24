using QRCoder;

namespace RestaurantMenu.Business.Abstract;

public interface IQrCodeService
{
    string CreateToken();

    byte[] GeneratePng(string url);
}

public class QrCodeManager : IQrCodeService
{
    public string CreateToken() => Guid.NewGuid().ToString("N");

    public byte[] GeneratePng(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(12);
    }
}
