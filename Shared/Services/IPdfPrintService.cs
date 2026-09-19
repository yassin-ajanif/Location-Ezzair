using GestionCommerciale.Shared.Models.Pdf;

namespace GestionCommerciale.Shared.Services;

public interface IPdfPrintService
{
    /// <summary>Preview/print a prebuilt PDF (A4 only — no paper-format picker).</summary>
    Task PrintPdfAsync(byte[] pdfBytes, string documentTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview/print with A4 / ticket 80mm / ticket 58mm. The builder is called when the user
    /// changes format; A4 should use <see cref="IPdfService"/>, tickets <see cref="ITicketPdfService"/>.
    /// </summary>
    Task PrintPdfAsync(
        Func<PrintPaperFormat, CancellationToken, Task<byte[]>> buildPdf,
        string documentTitle,
        CancellationToken cancellationToken = default);
}
