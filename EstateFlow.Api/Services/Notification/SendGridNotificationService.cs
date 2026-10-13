using System.Text;
using System.Text.Json;

namespace EstateFlow.Api.Services.Notifications;

// Strategy pattern — email via SendGrid today; a second strategy (e.g. SMS)
// could implement the same INotificationService without touching the Diary
// or ReportScheduler code that calls it, per Section 7.1 of the doc.
public class SendGridNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SendGridNotificationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task NotifyAsync(string recipientEmail, string subject, string body)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@estateflow.stbb.co.za";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Fail loudly rather than pretend the email went out. (The recipient's
            // address is deliberately not logged: POPIA.)
            throw new InvalidOperationException(
                "Email is not configured on the API. Set 'SendGrid__ApiKey' in the App Service settings.");
        }

        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = recipientEmail } } } },
            from = new { email = fromEmail },
            subject,
            content = new[] { new { type = "text/plain", value = body } }
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}