using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EstateFlow.Api.Services.Letters;

// Lays out a letter as an editable Word document on the firm's letterhead.
// Word is used rather than PDF so the controller can still adjust a letter
// before signing; Word can save it as PDF in one click.
public static class DocxLetterBuilder
{
    private const string Font = "Calibri";
    private const string AccentColour = "6E2A2A";

    public sealed record LetterContent(
        FirmOptions Firm,
        string OurReference,
        string MastersReference,
        IReadOnlyList<string> RecipientLines,
        string Subject,
        string Body,
        string SignatoryName,
        string SignatoryEmail);

    public static byte[] Build(LetterContent letter)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            AddStyles(main);

            var header = main.AddNewPart<HeaderPart>();
            header.Header = BuildLetterhead(letter.Firm);

            var footer = main.AddNewPart<FooterPart>();
            footer.Footer = BuildFooter(letter.Firm, letter.OurReference);

            var body = new Body();

            body.Append(Para(DateTime.Today.ToString("d MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-ZA")), after: 240));
            body.Append(Para($"Our ref: {letter.OurReference}", after: 0));
            body.Append(Para($"Master's ref: {letter.MastersReference}", after: 240));

            foreach (var line in letter.RecipientLines)
                body.Append(Para(line, after: 0));

            body.Append(Para(string.Empty, after: 120));
            body.Append(Para("Dear Sir / Madam", after: 240));
            body.Append(Para(letter.Subject.ToUpperInvariant(), bold: true, underline: true, after: 240));

            foreach (var paragraph in SplitParagraphs(letter.Body))
                body.Append(MultiLinePara(paragraph));

            body.Append(Para("Yours faithfully", after: 720));
            body.Append(Para(letter.SignatoryName, bold: true, after: 0));
            body.Append(Para("Estate Controller", after: 0));
            body.Append(Para($"{letter.Firm.Name} — {letter.Firm.Division}", after: 0));
            if (!string.IsNullOrWhiteSpace(letter.SignatoryEmail))
                body.Append(Para(letter.SignatoryEmail, after: 0));

            body.Append(new SectionProperties(
                new HeaderReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(header) },
                new FooterReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(footer) },
                new PageSize { Width = 11906U, Height = 16838U }, // A4
                new PageMargin { Top = 1700, Bottom = 1134, Left = 1247U, Right = 1247U, Header = 567U, Footer = 567U }));

            main.Document = new Document(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static Header BuildLetterhead(FirmOptions firm)
    {
        var header = new Header();

        header.Append(Para(firm.Name.ToUpperInvariant(), bold: true, size: 32, colour: AccentColour, after: 0));
        header.Append(Para(firm.Division, size: 20, colour: "6B6E76", after: 60));

        var contact = new List<string>();
        contact.AddRange(SplitLines(firm.Address));
        var reach = string.Join("  ·  ", new[] { firm.Phone, firm.Email, firm.Website }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (reach.Length > 0)
            contact.Add(reach);

        var details = Para(string.Join("  ·  ", contact), size: 16, colour: "6B6E76", after: 0);
        details.ParagraphProperties!.Append(new ParagraphBorders(
            new BottomBorder { Val = BorderValues.Single, Size = 8U, Space = 6U, Color = AccentColour }));
        header.Append(details);

        return header;
    }

    private static Footer BuildFooter(FirmOptions firm, string reference)
    {
        return new Footer(Para(
            $"{firm.Name}  ·  Ref {reference}  ·  This letter contains personal information and is intended only for the addressee.",
            size: 14, colour: "9CA0A8", after: 0));
    }

    private static Paragraph Para(
        string text,
        bool bold = false,
        bool underline = false,
        int size = 22,
        string? colour = null,
        int after = 160)
    {
        var runProperties = new RunProperties(
            new RunFonts { Ascii = Font, HighAnsi = Font, ComplexScript = Font },
            new FontSize { Val = size.ToString() });

        if (bold) runProperties.Append(new Bold());
        if (underline) runProperties.Append(new Underline { Val = UnderlineValues.Single });
        if (colour is not null) runProperties.Append(new Color { Val = colour });

        return new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = after.ToString(), Line = "276", LineRule = LineSpacingRuleValues.Auto }),
            new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    // A paragraph whose single line breaks are kept (e.g. "Make: ...").
    private static Paragraph MultiLinePara(string text)
    {
        var paragraph = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { After = "200", Line = "276", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Both }));

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var run = new Run(
                new RunProperties(new RunFonts { Ascii = Font, HighAnsi = Font }, new FontSize { Val = "22" }),
                new Text(lines[i].TrimEnd('\r')) { Space = SpaceProcessingModeValues.Preserve });

            if (i < lines.Length - 1)
                run.Append(new Break());

            paragraph.Append(run);
        }

        return paragraph;
    }

    private static IEnumerable<string> SplitParagraphs(string body) =>
        body.Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<string> SplitLines(string value) =>
        (value ?? string.Empty)
            .Replace("\\n", "\n")
            .Split(new[] { '\n', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void AddStyles(MainDocumentPart main)
    {
        var styles = main.AddNewPart<StyleDefinitionsPart>();
        styles.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(new RunPropertiesBaseStyle(
                    new RunFonts { Ascii = Font, HighAnsi = Font, ComplexScript = Font },
                    new FontSize { Val = "22" },
                    new Languages { Val = "en-ZA" })),
                new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
                    new SpacingBetweenLines { After = "160", Line = "276", LineRule = LineSpacingRuleValues.Auto }))));
        styles.Styles.Save();
    }
}
