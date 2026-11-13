using Discretion.Models;
using Discretion.Services.Pdf;

namespace Discretion.Services;

/// <summary>Generates agreement PDFs on the device and manages the resulting files.</summary>
public sealed class DocumentService
{
    private readonly string _root = Path.Combine(FileSystem.AppDataDirectory, "documents");

    public DocumentService() => Directory.CreateDirectory(_root);

    /// <summary>Renders the agreement to PDF, writes it to app storage and records the file name on the agreement.</summary>
    public async Task<string> GenerateAsync(Agreement agreement)
    {
        var doc = NdaComposer.Compose(agreement);
        var bytes = await Task.Run(() => AgreementPdfRenderer.Render(doc, agreement));
        var fileName = $"{agreement.ReferenceNumber}.pdf";
        var path = Path.Combine(_root, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        agreement.DocumentFileName = fileName;
        return path;
    }

    public string? GetPath(Agreement agreement)
    {
        if (agreement.DocumentFileName is null) return null;
        var path = Path.Combine(_root, agreement.DocumentFileName);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Returns a copy of the PDF in the cache directory, which is shareable on all platforms.</summary>
    public async Task<string> GetShareableCopyAsync(Agreement agreement)
    {
        var source = GetPath(agreement) ?? await GenerateAsync(agreement);
        var suffix = agreement.IsFullyExecuted ? "Executed" : "Unsigned";
        var target = Path.Combine(FileSystem.CacheDirectory, $"{agreement.ReferenceNumber}-{suffix}.pdf");
        File.Copy(source, target, overwrite: true);
        return target;
    }

    public void Delete(Agreement agreement)
    {
        if (GetPath(agreement) is { } path) File.Delete(path);
    }

    public static string DeviceDescription =>
        $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}".Trim();
}
