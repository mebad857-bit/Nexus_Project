namespace NexusCommunication.Services;

/// <summary>
/// Saves / deletes uploaded pictures under wwwroot/uploads/{folder}/.
/// Only real JPG / PNG / WEBP / GIF files are accepted (content is checked, not just the extension), max 3 MB.
/// Pictures that ship with the project (/images/...) are never deleted from disk - only uploads are.
/// </summary>
public class ImageStore
{
    public const long MaxBytes = 3 * 1024 * 1024;
    private readonly string _root;

    public ImageStore(IWebHostEnvironment env) => _root = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");

    /// <summary>resource name (API) -> upload sub folder</summary>
    public static readonly Dictionary<string, string> Folders = new()
    {
        ["customers"] = "customers", ["employees"] = "employees", ["vendors"] = "vendors",
        ["equipment"] = "products", ["shops"] = "shops", ["plans"] = "plans",
    };

    /// <summary>returns (webPath, null) on success or (null, errorMessage)</summary>
    public async Task<(string? Path, string? Error)> SaveAsync(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0) return (null, "Choose a picture first");
        if (file.Length > MaxBytes) return (null, "Picture is too large (max 3 MB)");

        var header = new byte[12];
        await using (var s = file.OpenReadStream()) { var n = await s.ReadAsync(header.AsMemory(0, 12)); if (n < 4) return (null, "That file is not a valid picture"); }

        string? ext = null;
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) ext = ".jpg";
        else if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) ext = ".png";
        else if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46) ext = ".gif";
        else if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) ext = ".webp";
        if (ext == null) return (null, "Only JPG, PNG, WEBP or GIF pictures are allowed");

        var dir = Path.Combine(_root, folder);
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + ext;
        await using (var fs = File.Create(Path.Combine(dir, name))) await file.CopyToAsync(fs);
        return ($"/uploads/{folder}/{name}", null);
    }

    /// <summary>delete a previously uploaded file (ignores /images/... seed pictures and anything outside /uploads/)</summary>
    public void Delete(string? webPath)
    {
        if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return;
        var rel = webPath["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_root, rel));
        if (!full.StartsWith(Path.GetFullPath(_root) + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return; // path traversal guard
        try { if (File.Exists(full)) File.Delete(full); } catch (IOException) { /* file in use - leave it */ }
    }
}
