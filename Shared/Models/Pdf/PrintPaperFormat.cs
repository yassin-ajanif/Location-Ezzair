namespace GestionCommerciale.Shared.Models.Pdf;

/// <summary>Paper choice in the Imprimer preview. A4 uses the existing commercial PDF; tickets use <see cref="Services.ITicketPdfService"/>.</summary>
public enum PrintPaperFormat
{
    A4 = 0,
    Ticket80mm = 1,
    Ticket58mm = 2
}

public static class PrintPaperFormatExtensions
{
    public const PrintPaperFormat Default = PrintPaperFormat.Ticket80mm;
    public static float TicketWidthMm(this PrintPaperFormat format) => format switch
    {
        PrintPaperFormat.Ticket80mm => 80f,
        PrintPaperFormat.Ticket58mm => 58f,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Not a ticket format.")
    };

    public static bool IsTicket(this PrintPaperFormat format) =>
        format is PrintPaperFormat.Ticket80mm or PrintPaperFormat.Ticket58mm;
}
