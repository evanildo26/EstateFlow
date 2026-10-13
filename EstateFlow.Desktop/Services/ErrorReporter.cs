using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;

namespace EstateFlow.Desktop.Services;

// One place that turns unexpected errors into a clear message instead of a
// crash, and keeps a local log for support. The log holds the technical
// details only (no estate data is written by EstateFlow itself).
public static class ErrorReporter
{
    private static readonly string LogFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EstateFlow", "logs");

    // Set by App: returns the user to the sign-in screen.
    public static Action? SessionExpired { get; set; }

    public static void Report(Exception exception, string? context = null)
    {
        Log(exception, context);

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
        {
            SessionExpired?.Invoke();
            Show("Your session has expired. Please sign in again.", MessageBoxImage.Information);
            return;
        }

        Show(Describe(exception), MessageBoxImage.Warning);
    }

    public static string Describe(Exception exception) => exception switch
    {
        TaskCanceledException or TimeoutException =>
            "The EstateFlow server took too long to respond. Check your connection and try again.",
        HttpRequestException { StatusCode: null } =>
            "Could not reach the EstateFlow server. Check your internet connection and try again.",
        HttpRequestException http =>
            http.Message,
        _ =>
            "Something went wrong. Please try again; if it keeps happening, contact support " +
            $"and mention the time ({DateTime.Now:HH:mm})."
    };

    public static void Log(Exception exception, string? context = null)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var file = Path.Combine(LogFolder, $"desktop-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never cause a second failure.
        }
    }

    private static void Show(string message, MessageBoxImage image)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;

        if (dispatcher.CheckAccess())
            MessageBox.Show(message, "EstateFlow", MessageBoxButton.OK, image);
        else
            dispatcher.BeginInvoke(() => MessageBox.Show(message, "EstateFlow", MessageBoxButton.OK, image));
    }
}
