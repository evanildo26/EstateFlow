using System.Globalization;
using System.Text;
using EstateFlow.Core.Models;
using EstateFlow.Core.Workflow;
using EstateFlow.Data.Context;
using EstateFlow.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EstateFlow.Functions;

// Monthly progress update to every executor and heir with an email address.
//  - Skips finalised and paused estates.
//  - Sends each estate at most once per calendar month, so a retried or
//    re-run timer never emails people twice.
//  - Records every email in the estate's correspondence history.
public class MonthlyReportFunction
{
    private static readonly CultureInfo SouthAfrica = CultureInfo.GetCultureInfo("en-ZA");

    private readonly EstateFlowDbContext _context;
    private readonly EmailNotificationService _notificationService;
    private readonly ILogger<MonthlyReportFunction> _logger;
    private readonly MasterProcessOptions _processOptions;
    private readonly string _firmName;
    private readonly string _division;

    public MonthlyReportFunction(
        EstateFlowDbContext context,
        EmailNotificationService notificationService,
        ILogger<MonthlyReportFunction> logger,
        IOptions<MasterProcessOptions> processOptions,
        IConfiguration configuration)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
        _processOptions = processOptions.Value;
        _firmName = configuration["Firm:Name"] ?? "STBB Incorporated";
        _division = configuration["Firm:Division"] ?? "Deceased Estates Division";
    }

    // Runs at 06:00 UTC on the 1st of every month.
    [Function("MonthlyReportFunction")]
    public async Task Run(
        [TimerTrigger("0 0 6 1 * *")] TimerInfo timer)
    {
        _logger.LogInformation(
            "MonthlyReportFunction started at: {time}",
            DateTime.UtcNow);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var estates =
            await _context.Estates
                .Include(e => e.Deceased)
                .Include(e => e.Controller)
                .Include(e => e.Executors)
                .Include(e => e.Beneficiaries)
                .Include(e => e.ProgressStages)
                .Include(e => e.AutomatedReports)
                .Include(e => e.Milestones)
                .AsSplitQuery()
                .ToListAsync();

        var sentCount = 0;
        var skippedCount = 0;

        foreach (var estate in estates)
        {
            var latestReport =
                estate.AutomatedReports
                    .OrderByDescending(r => r.ReportPeriod)
                    .FirstOrDefault();

            if (latestReport?.IsPaused == true)
            {
                skippedCount++;
                continue;
            }

            var progress = MasterProcess.Evaluate(
                estate.DateOfDeath,
                estate.Milestones.GroupBy(m => m.Stage).ToDictionary(g => g.Key, g => g.First().CompletedOn),
                DateTime.Today,
                _processOptions);

            if (progress.IsFinalised ||
                EstateStatuses.Normalize(estate.EstateStatus) == EstateStatuses.Finalised)
            {
                skippedCount++;
                continue;
            }

            if (estate.AutomatedReports.Any(r => r.SentDate >= monthStart))
            {
                _logger.LogInformation(
                    "Skipping estate {file} — this month's report was already sent.",
                    estate.FileNumber);

                skippedCount++;
                continue;
            }

            var recipients =
                estate.Executors
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .Select(x => (x.FullName, Email: x.Email!.Trim(), Type: "Executor"))
                    .Concat(
                        estate.Beneficiaries
                            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                            .Select(x => (x.FullName, Email: x.Email!.Trim(), Type: "Beneficiary")))
                    .GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

            if (recipients.Count == 0)
            {
                _logger.LogWarning(
                    "Estate {file} has no executor or beneficiary email addresses. No monthly report was sent.",
                    estate.FileNumber);

                skippedCount++;
                continue;
            }

            var recentUpdates =
                estate.ProgressStages
                    .Where(s => s.TargetDate.HasValue &&
                                s.TargetDate.Value >= now.AddDays(-35) &&
                                s.TargetDate.Value <= now)
                    .OrderByDescending(s => s.TargetDate)
                    .Take(6)
                    .ToList();

            var subject = $"Monthly estate update — Estate late {estate.Deceased?.FullName} (file {estate.FileNumber})";
            var sentForEstate = 0;

            foreach (var recipient in recipients)
            {
                var status = "Sent";

                try
                {
                    await _notificationService.NotifyAsync(
                        recipient.Email,
                        subject,
                        BuildText(recipient.FullName, estate, progress, recentUpdates),
                        BuildHtml(recipient.FullName, estate, progress, recentUpdates));

                    sentCount++;
                    sentForEstate++;
                }
                catch (Exception ex)
                {
                    status = "Failed";

                    _logger.LogError(
                        ex,
                        "Could not send monthly report for estate {file} to {recipient}.",
                        estate.FileNumber,
                        LogRedaction.MaskEmail(recipient.Email));
                }

                _context.Correspondence.Add(new CorrespondenceRecord
                {
                    EstateId = estate.EstateId,
                    Channel = "Email",
                    Kind = "Monthly report",
                    SentBy = "EstateFlow (automated)",
                    RecipientName = recipient.FullName,
                    RecipientEmail = recipient.Email,
                    Subject = subject,
                    Summary = $"{progress.PercentComplete}% complete. Next step: {progress.NextMilestone?.Title ?? "—"}.",
                    Status = status
                });
            }

            if (sentForEstate > 0)
            {
                _context.AutomatedReports.Add(new AutomatedReport
                {
                    EstateId = estate.EstateId,
                    ReportPeriod = now,
                    RecipientType = "Executor/Beneficiary",
                    SentDate = now,
                    IsPaused = false
                });
            }

            // Save per estate so a later failure can't lose the record of
            // emails that already went out.
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "MonthlyReportFunction completed. Estates: {estateCount}; emails sent: {sentCount}; estates skipped: {skippedCount}.",
            estates.Count,
            sentCount,
            skippedCount);
    }

    private string BuildText(
        string recipientName,
        Estate estate,
        EstateProgress progress,
        IReadOnlyList<ProgressStage> recentUpdates)
    {
        var text = new StringBuilder();

        text.AppendLine($"Dear {recipientName},");
        text.AppendLine();
        text.AppendLine($"Here is this month's update on the estate late {estate.Deceased?.FullName} (our file {estate.FileNumber}).");
        text.AppendLine();
        text.AppendLine($"Progress: {progress.PercentComplete}% ({progress.CompletedCount} of {progress.Milestones.Count} stages complete)");
        text.AppendLine($"Next step: {progress.NextMilestone?.Title ?? "—"}");
        text.AppendLine();
        text.AppendLine("STAGES");

        foreach (var m in progress.Milestones)
        {
            var mark = m.CompletedOn is DateTime done
                ? $"[x] {m.Title} — {done.ToString("d MMM yyyy", SouthAfrica)}"
                : $"[ ] {m.Title}";
            text.AppendLine(mark);
        }

        if (recentUpdates.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("RECENT UPDATES");
            foreach (var u in recentUpdates)
                text.AppendLine($"{u.TargetDate!.Value.ToString("d MMM yyyy", SouthAfrica)}: {u.StageName}");
        }

        text.AppendLine();
        text.AppendLine($"If you have any questions, please contact {estate.Controller?.StaffName ?? "your estate controller"}" +
                        (string.IsNullOrWhiteSpace(estate.Controller?.Email) ? "." : $" at {estate.Controller!.Email}."));
        text.AppendLine();
        text.AppendLine("Kind regards,");
        text.AppendLine($"{_firmName} — {_division}");

        return text.ToString();
    }

    private string BuildHtml(
        string recipientName,
        Estate estate,
        EstateProgress progress,
        IReadOnlyList<ProgressStage> recentUpdates)
    {
        var e = EmailLayout.Encode;
        var html = new StringBuilder();

        html.Append($"<p style=\"margin:0 0 14px\">Dear {e(recipientName)},</p>");
        html.Append($"<p style=\"margin:0 0 14px\">Here is this month's update on the <strong>estate late {e(estate.Deceased?.FullName)}</strong> (our file {e(estate.FileNumber)}).</p>");

        html.Append(EmailLayout.SectionTitle("Progress"));
        html.Append(EmailLayout.ProgressBar(progress.PercentComplete));
        html.Append($"<div style=\"font-size:14px;color:#6B6E76;\">{progress.PercentComplete}% complete · {progress.CompletedCount} of {progress.Milestones.Count} stages</div>");

        if (progress.NextMilestone is not null)
        {
            html.Append($"<p style=\"margin:12px 0 0;\">Next step: <strong style=\"color:#6E2A2A;\">{e(progress.NextMilestone.Title)}</strong><br>" +
                        $"<span style=\"font-size:13px;color:#6B6E76;\">{e(progress.NextMilestone.Description)}</span></p>");
        }

        html.Append(EmailLayout.SectionTitle("Stages"));
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"font-size:14px;\">");

        foreach (var m in progress.Milestones)
        {
            var done = m.CompletedOn is not null;
            var mark = done ? "&#10003;" : (m.IsNext ? "&#8594;" : "&#9675;");
            var colour = done ? "#3F5B45" : (m.IsNext ? "#6E2A2A" : "#9CA0A8");
            var when = m.CompletedOn?.ToString("d MMM yyyy", SouthAfrica) ?? (m.IsNext ? "In progress" : string.Empty);

            html.Append("<tr>");
            html.Append($"<td width=\"24\" style=\"padding:6px 0;color:{colour};font-weight:bold;vertical-align:top;\">{mark}</td>");
            html.Append($"<td style=\"padding:6px 0;border-bottom:1px solid #E4E2DD;color:{(done || m.IsNext ? "#16181C" : "#6B6E76")};\">{e(m.Title)}</td>");
            html.Append($"<td align=\"right\" style=\"padding:6px 0;border-bottom:1px solid #E4E2DD;color:#6B6E76;font-size:12px;white-space:nowrap;\">{e(when)}</td>");
            html.Append("</tr>");
        }

        html.Append("</table>");

        if (recentUpdates.Count > 0)
        {
            html.Append(EmailLayout.SectionTitle("Recent updates"));
            html.Append("<ul style=\"margin:0;padding-left:18px;font-size:14px;\">");
            foreach (var u in recentUpdates)
            {
                html.Append($"<li style=\"margin:0 0 6px;\"><span style=\"color:#9CA0A8;\">{e(u.TargetDate!.Value.ToString("d MMM yyyy", SouthAfrica))}</span> — {e(u.StageName)}</li>");
            }
            html.Append("</ul>");
        }

        var contact = estate.Controller?.StaffName ?? "your estate controller";
        html.Append($"<p style=\"margin:22px 0 14px;\">If you have any questions, please contact {e(contact)}");
        if (!string.IsNullOrWhiteSpace(estate.Controller?.Email))
            html.Append($" at <a href=\"mailto:{e(estate.Controller!.Email)}\" style=\"color:#6E2A2A;\">{e(estate.Controller!.Email)}</a>");
        html.Append(".</p>");

        html.Append($"<p style=\"margin:0;\">Kind regards,<br>{e(_firmName)} — {e(_division)}</p>");

        return EmailLayout.Wrap(
            _firmName,
            _division,
            $"{progress.PercentComplete}% complete — next step: {progress.NextMilestone?.Title}",
            html.ToString());
    }
}
