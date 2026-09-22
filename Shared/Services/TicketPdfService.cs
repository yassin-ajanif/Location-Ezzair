using System.Globalization;
using GestionCommerciale.Modules.AvoirFournisseur.Models;
using GestionCommerciale.Modules.CommandeFournisseur.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.FactureFournisseur.Models;
using GestionCommerciale.Modules.Reception.Models;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services.Pdf;
using QuestPDF.Infrastructure;

namespace GestionCommerciale.Shared.Services;

public sealed class TicketPdfService : ITicketPdfService
{
    private readonly IAppSettingsService _settings;

    public TicketPdfService(IAppSettingsService settings)
    {
        _settings = settings;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> BuildBonReceptionTicketAsync(BonReception br, DocumentPartyPdfInfo party, float widthMm, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var totals = DocumentTotalsHelper.BonReceptionTotals(br.Lignes);
        var lines = br.Lignes.Select(l =>
            LineTtc(l.Designation, l.QuantiteRecue, l.PrixUnitaireHT, 0, l.TauxTVA)).ToList();
        return Render(cfg, "BON DE RÉCEPTION", br.Numero, "Fournisseur", party.Nom, lines, totals.ttc, widthMm);
    }

    public async Task<byte[]> BuildBonCommandeTicketAsync(BonCommande bc, DocumentPartyPdfInfo party, float widthMm, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var totals = DocumentTotalsHelper.BonCommandeTotals(bc.Lignes);
        var lines = bc.Lignes.Select(l =>
            LineTtc(l.Designation, l.QuantiteCommandee, l.PrixUnitaireHT, l.Remise, l.TauxTVA)).ToList();
        return Render(cfg, "BON DE COMMANDE", bc.Numero, "Fournisseur", party.Nom, lines, totals.ttc, widthMm);
    }

    public async Task<byte[]> BuildFactureTicketAsync(Facture facture, DocumentPartyPdfInfo party, float widthMm, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var totals = DocumentTotalsHelper.FactureTotals(facture.Lignes, facture.RemiseGlobale);
        var lines = facture.Lignes.Select(l =>
            LineTtc(l.Designation, l.Quantite, l.PrixUnitaireHT, l.Remise, l.TauxTVA, l.RentedByDay, l.Days)).ToList();
        return Render(cfg, "FACTURE", facture.Numero, "Client", party.Nom, lines, totals.ttc, widthMm);
    }

    public async Task<byte[]> BuildFactureFournisseurTicketAsync(FactureFournisseur factureFournisseur, DocumentPartyPdfInfo party, float widthMm, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var totals = DocumentTotalsHelper.FactureFournisseurTotals(factureFournisseur.Lignes, factureFournisseur.RemiseGlobale);
        var lines = factureFournisseur.Lignes.Select(l =>
            LineTtc(l.Designation, l.Quantite, l.PrixUnitaireHT, l.Remise, l.TauxTVA)).ToList();
        return Render(cfg, "FACTURE FOURNISSEUR", factureFournisseur.Numero, "Fournisseur", party.Nom, lines, totals.ttc, widthMm);
    }

    public async Task<byte[]> BuildAvoirFournisseurTicketAsync(AvoirFournisseur doc, DocumentPartyPdfInfo party, float widthMm, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var totals = DocumentTotalsHelper.AvoirFournisseurTotals(doc.Lignes);
        var lines = doc.Lignes.Select(l =>
            LineTtc(l.Designation, l.Quantite, l.PrixUnitaireHT, l.Remise, l.TauxTVA)).ToList();
        return Render(cfg, "AVOIR FOURNISSEUR", doc.Numero, "Fournisseur", party.Nom, lines, totals.ttc, widthMm);
    }

    public async Task<byte[]> BuildBonSortieTicketAsync(BonSortie doc, DocumentPartyPdfInfo party, float widthMm, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var totals = DocumentTotalsHelper.BonSortieTotals(doc.ProduitLignes, doc.ServiceLignes, doc.RemiseGlobale);
        var lines = doc.ProduitLignes
            .Select(l => LineTtc(l.Designation, l.Quantite, l.PrixUnitaireHT, l.Remise, l.TauxTVA, l.RentedByDay, l.Days))
            .Concat(doc.ServiceLignes.Select(l =>
                LineTtc(l.Designation, l.Quantite, l.PrixUnitaireHT, l.Remise, l.TauxTVA)))
            .ToList();
        var periode =
            $"{FmtDate(doc.DateDebut)} → {FmtDate(doc.DateFinPrevue)}";
        return Render(cfg, "BON DE SORTIE", doc.Numero, "Client", party.Nom, lines, totals.ttc, widthMm,
            extraLabel: "Période",
            extraValue: periode);
    }

    /// <summary>Always LTR numeric dates — Arabic UI culture must not BiDi-scramble tickets.</summary>
    private static string FmtDate(DateTime value) =>
        value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string FmtDateTime(DateTime value) =>
        value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    private static TicketLinePdfModel LineTtc(
        string designation,
        decimal qty,
        decimal puHt,
        decimal remise,
        decimal tauxTva,
        bool rentedByDay = false,
        int? days = null)
    {
        var billingDays = DocumentTotalsHelper.EffectiveBillingDays(rentedByDay, days);
        var label = rentedByDay ? $"{designation} ({billingDays} j)" : designation;
        var montantHt = DocumentTotalsHelper.LigneHT(qty, puHt, remise, rentedByDay, days);
        return new TicketLinePdfModel
        {
            Designation = label,
            Quantite = qty,
            PrixUnitaire = DocumentTotalsHelper.PrixUnitaireTtc(puHt, tauxTva),
            Montant = montantHt * (1 + tauxTva / 100m)
        };
    }

    private static byte[] Render(
        AppSettingsRow cfg,
        string kind,
        string numero,
        string partyLabel,
        string partyName,
        IReadOnlyList<TicketLinePdfModel> lines,
        decimal total,
        float widthMm,
        string? extraLabel = null,
        string? extraValue = null)
    {
        if (widthMm is not (58f or 80f))
            throw new ArgumentOutOfRangeException(nameof(widthMm), "Ticket width must be 58 or 80 mm.");

        var devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise.Trim();
        var model = new TicketDocumentPdfModel
        {
            CompanyName = cfg.SocieteNom ?? string.Empty,
            CompanyInfoLines = BuildCompanyInfoLines(cfg),
            LogoBytes = TryLoadLogoBytes(cfg.SocieteLogoPath),
            DocumentKindLabel = kind,
            Numero = numero,
            DateText = FmtDateTime(DateTime.Now),
            ExtraInfoLabel = extraLabel,
            ExtraInfoValue = extraValue,
            PartyLabel = partyLabel,
            PartyName = string.IsNullOrWhiteSpace(partyName) ? "—" : partyName,
            Lines = lines,
            Total = total,
            Devise = devise,
            WidthMm = widthMm
        };
        return TicketPdfRenderer.Render(model);
    }

    private static IReadOnlyList<string> BuildCompanyInfoLines(AppSettingsRow cfg)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(cfg.SocieteAdresse))
            lines.Add(cfg.SocieteAdresse.Trim());
        if (!string.IsNullOrWhiteSpace(cfg.SocieteICE))
            lines.Add($"ICE : {cfg.SocieteICE.Trim()}");

        if (!string.IsNullOrWhiteSpace(cfg.SocieteMentionsLegales))
        {
            foreach (var part in cfg.SocieteMentionsLegales.Split(
                         '\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                lines.Add(part);
        }

        return lines;
    }

    private static byte[]? TryLoadLogoBytes(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }
        catch
        {
            return null;
        }
    }
}
