namespace GestionCommerciale.Shared.Models.Pdf;

public sealed class TicketLinePdfModel
{
    public required string Designation { get; init; }
    public required decimal Quantite { get; init; }
    public required decimal PrixUnitaire { get; init; }
    public required decimal Montant { get; init; }
}

public sealed class TicketDocumentPdfModel
{
    public string CompanyName { get; init; } = string.Empty;
    public IReadOnlyList<string> CompanyInfoLines { get; init; } = Array.Empty<string>();
    public byte[]? LogoBytes { get; init; }
    public required string DocumentKindLabel { get; init; }
    public required string Numero { get; init; }
    public required string DateText { get; init; }
    public string? ExtraInfoLabel { get; init; }
    public string? ExtraInfoValue { get; init; }
    public required string PartyLabel { get; init; }
    public required string PartyName { get; init; }
    public IReadOnlyList<TicketLinePdfModel> Lines { get; init; } = Array.Empty<TicketLinePdfModel>();
    public required decimal Total { get; init; }
    public required string Devise { get; init; }
    public string FooterMessage { get; init; } = "Merci de votre visite";
    public required float WidthMm { get; init; }
}
