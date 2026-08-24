namespace RestaurantMenu.WebUI.Infrastructure;

public static class ImageUpload
{
    private static readonly HashSet<string> AllowedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "products", "logos"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public static async Task<string?> SaveAsync(IFormFile? file, string folder, IWebHostEnvironment env)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            throw new InvalidOperationException("Görsel en fazla 2 MB olabilir.");
        }

        folder = Path.GetFileName(folder);
        if (!AllowedFolders.Contains(folder))
        {
            throw new InvalidOperationException("Geçersiz yükleme klasörü.");
        }

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("Yalnızca jpg, png veya webp yüklenebilir.");
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        if (!IsAllowedImage(bytes, ext))
        {
            throw new InvalidOperationException("Dosya içeriği görsel formatıyla uyuşmuyor.");
        }

        var dir = Path.Combine(env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var path = Path.Combine(dir, name);
        await File.WriteAllBytesAsync(path, bytes);
        return $"/uploads/{folder}/{name}";
    }

    private static bool IsAllowedImage(byte[] header, string ext)
    {
        if (header.Length < 4)
        {
            return false;
        }

        var jpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var png = header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var webp = header.Length >= 12
            && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
            && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';

        return ext.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => jpeg,
            ".png" => png,
            ".webp" => webp,
            _ => false
        };
    }
}
