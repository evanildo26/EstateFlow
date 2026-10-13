using System.Text;
using System.Text.Encodings.Web;

namespace EstateFlow.Functions.Services;

// HTML email wrapper with the firm's letterhead. Email clients ignore
// <style> blocks unreliably, so all styling is inline. Every value that comes
// from the database goes through Encode().
public static class EmailLayout
{
    private const string Accent = "#6E2A2A";
    private const string Ink = "#16181C";
    private const string Muted = "#6B6E76";
    private const string Line = "#E4E2DD";

    public static string Encode(string? value) =>
        HtmlEncoder.Default.Encode(value ?? string.Empty);

    // Plain text with line breaks → safe HTML paragraphs.
    public static string Paragraphs(string? text)
    {
        var html = new StringBuilder();

        foreach (var paragraph in (text ?? string.Empty)
                     .Replace("\r\n", "\n")
                     .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            html.Append($"<p style=\"margin:0 0 14px\">{Encode(paragraph).Replace("\n", "<br>")}</p>");
        }

        return html.ToString();
    }

    public static string Wrap(string firmName, string division, string preheader, string innerHtml)
    {
        return $"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width"></head>
            <body style="margin:0;padding:0;background:#F5F4F1;">
            <span style="display:none;max-height:0;overflow:hidden;">{Encode(preheader)}</span>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#F5F4F1;padding:24px 12px;">
            <tr><td align="center">
            <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#FFFFFF;border:1px solid {Line};border-radius:6px;">
              <tr><td style="padding:22px 28px;border-bottom:3px solid {Accent};">
                <div style="font:600 20px Georgia,'Times New Roman',serif;color:{Ink};letter-spacing:.02em;">{Encode(firmName)}</div>
                <div style="font:13px Arial,sans-serif;color:{Muted};margin-top:2px;">{Encode(division)}</div>
              </td></tr>
              <tr><td style="padding:24px 28px;font:15px/1.55 Arial,sans-serif;color:{Ink};">
                {innerHtml}
              </td></tr>
              <tr><td style="padding:14px 28px;border-top:1px solid {Line};font:12px/1.5 Arial,sans-serif;color:#9CA0A8;">
                This email contains personal information about a deceased estate and is intended only for the addressee.
                If you received it in error, please let us know and delete it.
              </td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;
    }

    public static string SectionTitle(string text) =>
        $"<div style=\"font:600 11px Arial,sans-serif;letter-spacing:.08em;text-transform:uppercase;color:#9CA0A8;margin:22px 0 8px;\">{Encode(text)}</div>";

    public static string ProgressBar(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        var rest = 100 - percent;

        return $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:8px 0 4px;border-radius:4px;overflow:hidden;">
              <tr>
                {(percent > 0 ? $"<td width=\"{percent}%\" style=\"height:8px;background:#3F5B45;font-size:0;\">&nbsp;</td>" : "")}
                {(rest > 0 ? $"<td width=\"{rest}%\" style=\"height:8px;background:#ECEAE5;font-size:0;\">&nbsp;</td>" : "")}
              </tr>
            </table>
            """;
    }
}
