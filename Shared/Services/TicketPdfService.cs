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
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace GestionCommerciale.Shared.Services;

public sealed class TicketPdfService : ITicketPdfService
{
    private readonly IAppSettingsService _settings;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public TicketPdfService(IAppSettingsService settings, IDbContextFactory<AppDbContext> dbFactory)
    {
        _settings = settings;
        _dbFactory = dbFactory;
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

        var extras = new List<TicketHeaderExtra>();
        var linked = await LoadLinkedBonSortiesAsync(facture, cancellationToken);
        if (linked.Count > 0)
        {
            extras.Add(new TicketHeaderExtra
            {
                Label = "Bon de sortie",
                Value = string.Join(", ", linked.Select(b => b.Numero))
            });
            var debut = linked.Min(b => b.DateDebut);
            var fin = linked.Max(b => b.DateFinPrevue);
            extras.Add(new TicketHeaderExtra
            {
                Label = "Période",
                Value = $"{FmtDate(debut)} → {FmtDate(fin)}"
            });
        }

        return Render(cfg, "FACTURE", facture.Numero, "Client", party.Nom, lines, totals.ttc, widthMm,
            headerExtras: extras,
            statusText: facture.EstPayee ? "Payée" : "Non payée");
    }

    public async Task<byte[]> BuildFactureFournisseurTicketAsync(FactureFournisseur factureFournisseur, DocumentPartyPdfInfo party, float widthMm, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var totals = DocumentTotalsHelper.FactureFournisseurTotals(factureFournisseur.Lignes, factureFournisseur.RemiseGlobale);
        var lines = factureFournisseur.Lignes.Select(l =>
            LineTtc(l.Designation, l.Quantite, l.PrixUnitaireHT, l.Remise, l.TauxTVA)).ToList();
        return Render(cfg, "FACTURE FOURNISSEUR", factureFournisseur.Numero, "Fournisseur", party.Nom, lines, totals.ttc, widthMm,
            statusText: factureFournisseur.EstPayee ? "Payée" : "Non payée");
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
        var extras = new[]
        {
            new TicketHeaderExtra
            {
                Label = "Période",
                Value = $"{FmtDate(doc.DateDebut)} → {FmtDate(doc.DateFinPrevue)}"
            }
        };
        return Render(cfg, "BON DE SORTIE", doc.Numero, "Client", party.Nom, lines, totals.ttc, widthMm,
            headerExtras: extras);
    }

    /// <summary>Linked BS via FactureId, else via line BonSortieId snapshots.</summary>
    private async Task<IReadOnlyList<(string Numero, DateTime DateDebut, DateTime DateFinPrevue)>> LoadLinkedBonSortiesAsync(
        Facture facture,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (facture.Id > 0)
        {
            var byFacture = await db.BonsSortie.AsNoTracking()
                .Where(b => b.FactureId == facture.Id)
                .OrderBy(b => b.Date).ThenBy(b => b.Numero)
                .Select(b => new { b.Numero, b.DateDebut, b.DateFinPrevue })
                .ToListAsync(cancellationToken);
            if (byFacture.Count > 0)
                return byFacture.Select(b => (b.Numero, b.DateDebut, b.DateFinPrevue)).ToList();
        }

        var lineIds = facture.Lignes
            .Where(l => l.BonSortieId is > 0)
            .Select(l => l.BonSortieId!.Value)
            .Distinct()
            .ToList();
        if (lineIds.Count == 0)
            return Array.Empty<(string, DateTime, DateTime)>();

        var byLines = await db.BonsSortie.AsNoTracking()
            .Where(b => lineIds.Contains(b.Id))
            .OrderBy(b => b.Date).ThenBy(b => b.Numero)
            .Select(b => new { b.Numero, b.DateDebut, b.DateFinPrevue })
            .ToListAsync(cancellationToken);
        return byLines.Select(b => (b.Numero, b.DateDebut, b.DateFinPrevue)).ToList();
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
        IReadOnlyList<TicketHeaderExtra>? headerExtras = null,
        string? statusText = null)
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
            HeaderExtras = headerExtras ?? Array.Empty<TicketHeaderExtra>(),
            PartyLabel = partyLabel,
            PartyName = string.IsNullOrWhiteSpace(partyName) ? "—" : partyName,
            StatusText = statusText,
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
