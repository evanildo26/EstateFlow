using System.IO;

namespace EstateFlow.Desktop.Services;

// Documents opened from EstateFlow (death certificates, ID copies, letters)
// are written to a private sub-folder of the Windows temp folder and removed
// on start-up, sign-out and exit, so personal information doesn't pile up
// on the workstation (POPIA).
public static class TempFiles
{
    public static string Folder { get; } =
        Path.Combine(Path.GetTempPath(), "EstateFlow");

    public static string PathFor(string fileName)
    {
        Directory.CreateDirectory(Folder);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = $"{Guid.NewGuid():N}.tmp";

        var path = Path.Combine(Folder, safeName);
        if (!File.Exists(path))
            return path;

        // Same name already open (e.g. in Word): keep both.
        var stem = Path.GetFileNameWithoutExtension(safeName);
        var extension = Path.GetExtension(safeName);
        return Path.Combine(Folder, $"{stem}_{DateTime.Now:HHmmss}_{Guid.NewGuid().ToString("N")[..4]}{extension}");
    }

    // Best effort: files still open in another program are skipped and
    // removed next time.
    public static void CleanUp()
    {
        if (!Directory.Exists(Folder))
            return;

        foreach (var file in Directory.EnumerateFiles(Folder))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
