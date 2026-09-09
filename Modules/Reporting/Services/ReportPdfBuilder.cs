using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionCommerciale.Modules.Reporting.Services;

public sealed record ReportPdfSummaryLine(string Label, string Value);

public sealed class ReportPdfModel
{
    public required string Title { get; init; }
    public required string PeriodText { get; init; }
    public required string CompanyName { get; init; }
    public IReadOnlyList<ReportPdfSummaryLine> Summary { get; init; } = [];
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}

public static class ReportPdfBuilder
{
    static ReportPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(ReportPdfModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor("#111827"));

                page.Header().Column(col =>
                {
                    col.Item().Text(model.CompanyName).SemiBold().FontSize(12).FontColor("#374151");
                    col.Item().PaddingTop(4).Text(model.Title).SemiBold().FontSize(16);
                    col.Item().PaddingTop(2).Text(model.PeriodText).FontSize(10).FontColor("#6B7280");
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#E5E7EB");
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    if (model.Summary.Count > 0)
                    {
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            foreach (var s in model.Summary)
                            {
                                row.RelativeItem().PaddingRight(8).Border(1).BorderColor("#E5E7EB")
                                    .Background("#F9FAFB").Padding(8).Column(c =>
                                    {
                                        c.Item().Text(s.Label).FontSize(8).FontColor("#6B7280");
                                        c.Item().PaddingTop(2).Text(s.Value).SemiBold().FontSize(10);
                                    });
                            }
                        });
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in model.Columns)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in model.Columns)
                            {
                                header.Cell().Background("#E5E7EB").Padding(5)
                                    .Text(h).SemiBold().FontSize(8);
                            }
                        });

                        var i = 0;
                        foreach (var row in model.Rows)
                        {
                            var bg = i++ % 2 == 0 ? "#FFFFFF" : "#F9FAFB";
                            foreach (var cell in row)
                            {
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E5E7EB")
                                    .Padding(4).Text(cell ?? string.Empty).FontSize(8);
                            }
                        }
                    });

                    if (model.Rows.Count == 0)
                        col.Item().PaddingTop(16).Text("—").FontColor("#9CA3AF");
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor("#9CA3AF");
                    text.CurrentPageNumber().FontSize(8).FontColor("#9CA3AF");
                    text.Span(" / ").FontSize(8).FontColor("#9CA3AF");
                    text.TotalPages().FontSize(8).FontColor("#9CA3AF");
                });
            });
        }).GeneratePdf();
    }
}
