using GestionCommerciale.Shared.Models.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace GestionCommerciale.Shared.Services.Pdf;

/// <summary>Thermal ticket layout (58/80 mm). Independent from A4 <see cref="CommercialDocumentPdfRenderer"/>.</summary>
public static class TicketPdfRenderer
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public static byte[] Render(TicketDocumentPdfModel model)
    {
        if (model.WidthMm is not (58f or 80f))
            throw new ArgumentOutOfRangeException(nameof(model), "Ticket width must be 58 or 80 mm.");

        var is80 = model.WidthMm >= 80f;
        var dash = is80 ? new string('-', 42) : new string('-', 32);
        // Wide logo: small side inset, taller box so the brand fills most of the ticket width.
        var logoSideInsetMm = is80 ? 2f : 1.5f;
        var logoMaxHeightMm = is80 ? 30f : 22f;
        var bodySize = is80 ? 8.5f : 8f;
        var titleSize = is80 ? 12f : 11f;
        var totalSize = is80 ? 10f : 9.5f;
        var footerSize = is80 ? 8f : 7.5f;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Height follows content — avoids the empty white strip at the bottom of thermal tickets.
                page.ContinuousSize(model.WidthMm, Unit.Millimetre);
                page.MarginHorizontal(4);
                page.MarginVertical(6);
                // Tickets are French/LTR layouts; Arabic UI culture must not reverse digit runs in dates.
                page.ContentFromLeftToRight();
                // Bold by default: thermal printers wash out regular weight.
                page.DefaultTextStyle(x => x.FontSize(bodySize).FontColor(Colors.Black).Bold());

                page.Content().Column(col =>
                {
                    DrawLogo(col, model, logoSideInsetMm, logoMaxHeightMm);

                    col.Item().PaddingTop(4).AlignCenter().Text(dash).FontSize(7);
                    col.Item().PaddingTop(4).AlignCenter()
                        .Text(model.DocumentKindLabel).Bold().FontSize(titleSize);
                    col.Item().PaddingTop(6).Text($"N° : {AsLtr(model.Numero)}").Bold().FontSize(bodySize);
                    col.Item().Text($"Date : {AsLtr(model.DateText)}").Bold().FontSize(bodySize);
                    foreach (var extra in model.HeaderExtras)
                    {
                        if (string.IsNullOrWhiteSpace(extra.Label) || string.IsNullOrWhiteSpace(extra.Value))
                            continue;
                        col.Item().Text($"{extra.Label} : {AsLtr(extra.Value)}").Bold().FontSize(bodySize);
                    }
                    col.Item().Text($"{model.PartyLabel} : {model.PartyName}").Bold().FontSize(bodySize);
                    if (!string.IsNullOrWhiteSpace(model.StatusText))
                        col.Item().Text($"Statut : {model.StatusText}").Bold().FontSize(bodySize);
                    col.Item().PaddingTop(6);

                    DrawLinesTable(col, model);

                    col.Item().PaddingVertical(4).AlignCenter().Text(dash).FontSize(7);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL").Bold().FontSize(totalSize);
                        r.AutoItem().Text($"{FmtMoney(model.Total)} {model.Devise}").Bold().FontSize(totalSize);
                    });
                    col.Item().PaddingVertical(4).AlignCenter().Text(dash).FontSize(7);
                    col.Item().AlignCenter().Text(model.FooterMessage).Bold().FontSize(footerSize);

                    DrawCompanyInfoFooter(col, model, footerSize);
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void DrawLinesTable(ColumnDescriptor col, TicketDocumentPdfModel model)
    {
        // ~45% article / rest for numbers (mm) so Prix/QTE/Montant never wrap on 58/80.
        var is80 = model.WidthMm >= 80f;
        var prixW = is80 ? 16f : 13f;
        var qteW = is80 ? 9f : 7f;
        var montantW = is80 ? 17f : 14f;
        var headerSize = is80 ? 7.5f : 7f;
        var cellSize = is80 ? 7.5f : 7f;

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(prixW, Unit.Millimetre);
                columns.ConstantColumn(qteW, Unit.Millimetre);
                columns.ConstantColumn(montantW, Unit.Millimetre);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).AlignLeft().Text("Article").Bold().FontSize(headerSize);
                header.Cell().Element(HeaderCell).AlignRight().Text("Prix").Bold().FontSize(headerSize);
                header.Cell().Element(HeaderCell).AlignRight().Text("QTE").Bold().FontSize(headerSize);
                header.Cell().Element(HeaderCell).AlignRight().Text("Montant").Bold().FontSize(headerSize);
            });

            foreach (var line in model.Lines)
            {
                table.Cell().Element(BodyCell).AlignLeft()
                    .Text(line.Designation).Bold().FontSize(cellSize);
                table.Cell().Element(BodyCell).AlignRight()
                    .Text(FmtMoney(line.PrixUnitaire)).Bold().FontSize(cellSize);
                table.Cell().Element(BodyCell).AlignRight()
                    .Text(FmtQty(line.Quantite)).Bold().FontSize(cellSize);
                table.Cell().Element(BodyCell).AlignRight()
                    .Text(FmtMoney(line.Montant)).Bold().FontSize(cellSize);
            }
        });

        static IContainer HeaderCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(2).PaddingHorizontal(1);

        static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(1);
    }

    private static void DrawLogo(ColumnDescriptor col, TicketDocumentPdfModel model, float sideInsetMm, float maxHeightMm)
    {
        if (model.LogoBytes is { Length: > 0 })
        {
            col.Item()
                .PaddingHorizontal(sideInsetMm, Unit.Millimetre)
                .Height(maxHeightMm, Unit.Millimetre)
                .AlignCenter()
                .Image(model.LogoBytes)
                .FitArea();
            return;
        }

        // Fallback when no logo file is configured.
        if (!string.IsNullOrWhiteSpace(model.CompanyName))
        {
            col.Item().AlignCenter()
                .Text(model.CompanyName.Trim().ToUpperInvariant())
                .Bold()
                .FontSize(10);
        }
    }

    private static void DrawCompanyInfoFooter(ColumnDescriptor col, TicketDocumentPdfModel model, float footerSize)
    {
        var hasName = !string.IsNullOrWhiteSpace(model.CompanyName) && model.LogoBytes is { Length: > 0 };
        var infoLines = model.CompanyInfoLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (!hasName && infoLines.Count == 0)
            return;

        col.Item().PaddingTop(6);

        if (hasName)
        {
            col.Item().AlignCenter()
                .Text(model.CompanyName.Trim().ToUpperInvariant())
                .Bold()
                .FontSize(footerSize);
        }

        foreach (var info in infoLines)
        {
            col.Item().AlignCenter()
                .Text(info.Trim())
                .Bold()
                .FontSize(footerSize - 0.5f)
                .FontColor(Colors.Black);
        }
    }

    private static string FmtQty(decimal value) => value.ToString("0.###", Fr);
    private static string FmtMoney(decimal value) => value.ToString("N2", Fr);

    /// <summary>Unicode LRI…PDI — keeps digit/date runs LTR even if ambient culture is Arabic.</summary>
    private static string AsLtr(string value) =>
        string.IsNullOrEmpty(value) ? value : $"\u2066{value}\u2069";
}
