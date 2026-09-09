using System.Globalization;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reporting.Services;

public sealed class DashboardAlertsService : IDashboardAlertsService
{
    /// <summary>Days past fin prévue before a late return becomes a "long overdue" alert.</summary>
    public const int LongOverdueDays = 7;

    private const int SoftStartsWithinDays = 3;
    private const int ConflictHorizonDays = 45;
    private const int HighDemandHorizonDays = 7;
    private const int MaxPerKind = 15;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;

    public DashboardAlertsService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILocaleService locale,
        IAppSettingsService settings)
    {
        _dbFactory = dbFactory;
        _locale = locale;
        _settings = settings;
    }

    public async Task<IReadOnlyList<DashboardAlertRow>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var alerts = new List<DashboardAlertRow>();

        await AddBonSortieReturnAlertsAsync(db, today, alerts, cancellationToken);
        await AddSoftReservationStartingSoonAsync(db, today, alerts, cancellationToken);
        await AddAvailabilityAndDemandAlertsAsync(db, today, alerts, cancellationToken);
        await AddReturnConditionAlertsAsync(db, today, alerts, cancellationToken);
        await AddStockBelowMinAsync(db, alerts, cancellationToken);
        await AddUnpaidOverdueAsync(db, today, alerts, cancellationToken);

        return alerts
            .OrderBy(a => a.Severity)
            .ThenBy(a => a.Kind)
            .ThenBy(a => a.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task AddBonSortieReturnAlertsAsync(
        AppDbContext db,
        DateTime today,
        List<DashboardAlertRow> alerts,
        CancellationToken ct)
    {
        var openFlat = await (
            from l in db.BonSortieProduitLignes.AsNoTracking()
            join b in db.BonsSortie.AsNoTracking() on l.BonSortieId equals b.Id
            join t in db.Tiers.AsNoTracking() on b.ClientId equals t.Id
            where l.Quantite > l.QuantiteRetournee
            select new
            {
                b.Id,
                b.Numero,
                b.DateFinPrevue,
                Client = t.Nom,
                Encore = l.Quantite - l.QuantiteRetournee
            }).ToListAsync(ct);

        var open = openFlat
            .GroupBy(x => new { x.Id, x.Numero, Fin = x.DateFinPrevue.Date, x.Client })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Numero,
                DateFinPrevue = g.Key.Fin,
                g.Key.Client,
                Encore = g.Sum(x => x.Encore)
            })
            .ToList();

        var overdueCount = 0;
        var longCount = 0;
        var dueSoonCount = 0;

        foreach (var row in open.OrderBy(x => x.DateFinPrevue))
        {
            var fin = row.DateFinPrevue.Date;
            var daysLate = (today - fin).Days;
            var client = string.IsNullOrWhiteSpace(row.Client) ? $"#{row.Id}" : row.Client;
            var qty = row.Encore.ToString("N0", CultureInfo.CurrentCulture);

            if (daysLate >= LongOverdueDays)
            {
                if (longCount >= MaxPerKind) continue;
                longCount++;
                alerts.Add(new DashboardAlertRow(
                    DashboardAlertKind.LongOverdueMaterial,
                    DashboardAlertSeverity.Critical,
                    _locale.T("DashAlert_CatOps"),
                    _locale.Tf("DashAlert_LongOverdueTitle", row.Numero),
                    _locale.Tf("DashAlert_LongOverdueDetail", client, daysLate.ToString(CultureInfo.CurrentCulture), qty),
                    DashboardAlertNav.BonSortie,
                    row.Id));
            }
            else if (daysLate >= 1)
            {
                if (overdueCount >= MaxPerKind) continue;
                overdueCount++;
                alerts.Add(new DashboardAlertRow(
                    DashboardAlertKind.ReturnOverdue,
                    DashboardAlertSeverity.Critical,
                    _locale.T("DashAlert_CatOps"),
                    _locale.Tf("DashAlert_OverdueTitle", row.Numero),
                    _locale.Tf("DashAlert_OverdueDetail", client, daysLate.ToString(CultureInfo.CurrentCulture), qty),
                    DashboardAlertNav.BonSortie,
                    row.Id));
            }
            else if (fin == today || fin == today.AddDays(1))
            {
                if (dueSoonCount >= MaxPerKind) continue;
                dueSoonCount++;
                var when = fin == today
                    ? _locale.T("DashAlert_DueToday")
                    : _locale.T("DashAlert_DueTomorrow");
                alerts.Add(new DashboardAlertRow(
                    DashboardAlertKind.ReturnDueSoon,
                    DashboardAlertSeverity.Warning,
                    _locale.T("DashAlert_CatOps"),
                    _locale.Tf("DashAlert_DueSoonTitle", row.Numero),
                    _locale.Tf("DashAlert_DueSoonDetail", client, when, qty),
                    DashboardAlertNav.BonSortie,
                    row.Id));
            }
        }
    }

    private async Task AddSoftReservationStartingSoonAsync(
        AppDbContext db,
        DateTime today,
        List<DashboardAlertRow> alerts,
        CancellationToken ct)
    {
        var until = today.AddDays(SoftStartsWithinDays);
        var rows = await (
            from r in db.Reservations.AsNoTracking()
            join t in db.Tiers.AsNoTracking() on r.ClientId equals t.Id
            where r.Statut == StatutReservation.Confirmee
                  && r.DateDebut.Date >= today
                  && r.DateDebut.Date <= until
            orderby r.DateDebut
            select new { r.Id, r.Numero, r.DateDebut, Client = t.Nom }
        ).Take(MaxPerKind).ToListAsync(ct);

        foreach (var row in rows)
        {
            var client = string.IsNullOrWhiteSpace(row.Client) ? $"#{row.Id}" : row.Client;
            var days = (row.DateDebut.Date - today).Days;
            var when = days == 0
                ? _locale.T("DashAlert_DueToday")
                : days == 1
                    ? _locale.T("DashAlert_DueTomorrow")
                    : _locale.Tf("DashAlert_InDays", days.ToString(CultureInfo.CurrentCulture));

            alerts.Add(new DashboardAlertRow(
                DashboardAlertKind.SoftReservationStartingSoon,
                DashboardAlertSeverity.Info,
                _locale.T("DashAlert_CatOps"),
                _locale.Tf("DashAlert_SoftStartTitle", row.Numero),
                _locale.Tf("DashAlert_SoftStartDetail", client, when),
                DashboardAlertNav.SoftReservation,
                row.Id));
        }
    }

    private async Task AddAvailabilityAndDemandAlertsAsync(
        AppDbContext db,
        DateTime today,
        List<DashboardAlertRow> alerts,
        CancellationToken ct)
    {
        var conflictEnd = today.AddDays(ConflictHorizonDays);
        var demandEnd = today.AddDays(HighDemandHorizonDays);

        var openLines = await (
            from l in db.BonSortieProduitLignes.AsNoTracking()
            join b in db.BonsSortie.AsNoTracking() on l.BonSortieId equals b.Id
            where l.ProduitId != null && l.Quantite > l.QuantiteRetournee
            select new
            {
                ProduitId = l.ProduitId!.Value,
                DateDebut = b.DateDebut.Date,
                Encore = l.Quantite - l.QuantiteRetournee,
                OpenEnded = true
            }).ToListAsync(ct);

        var softLines = await (
            from l in db.ReservationProduitLignes.AsNoTracking()
            join r in db.Reservations.AsNoTracking() on l.ReservationId equals r.Id
            where r.Statut == StatutReservation.Confirmee
                  && l.ProduitId != null
                  && l.Quantite > 0
            select new
            {
                ProduitId = l.ProduitId!.Value,
                DateDebut = r.DateDebut.Date,
                DateFin = r.DateFinPrevue.Date,
                Encore = l.Quantite,
                OpenEnded = false
            }).ToListAsync(ct);

        var produitIds = openLines.Select(x => x.ProduitId)
            .Concat(softLines.Select(x => x.ProduitId))
            .Distinct()
            .ToList();
        if (produitIds.Count == 0)
            return;

        var produits = await db.Produits.AsNoTracking()
            .Where(p => produitIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Reference, p.Designation, p.StockActuel })
            .ToDictionaryAsync(p => p.Id, ct);

        var ownedByProduit = new Dictionary<int, decimal>();
        foreach (var pid in produitIds)
        {
            var stock = produits.TryGetValue(pid, out var p) ? p.StockActuel : 0m;
            var outQty = openLines.Where(l => l.ProduitId == pid).Sum(l => l.Encore);
            ownedByProduit[pid] = stock + outQty;
        }

        var conflictCount = 0;
        var demandCount = 0;
        var conflictedProducts = new HashSet<int>();

        foreach (var pid in produitIds)
        {
            if (!produits.TryGetValue(pid, out var prod))
                continue;

            var owned = ownedByProduit.GetValueOrDefault(pid);
            if (owned <= 0)
                continue;

            decimal maxConflictBooked = 0;
            DateTime? conflictDay = null;
            decimal maxDemandBooked = 0;
            DateTime? demandDay = null;

            for (var d = today; d <= conflictEnd; d = d.AddDays(1))
            {
                var booked = 0m;
                foreach (var l in openLines.Where(x => x.ProduitId == pid))
                {
                    if (d >= l.DateDebut)
                        booked += l.Encore;
                }

                foreach (var l in softLines.Where(x => x.ProduitId == pid))
                {
                    var end = l.DateFin;
                    if (end < l.DateDebut) end = l.DateDebut;
                    if (d >= l.DateDebut && d <= end)
                        booked += l.Encore;
                }

                if (booked > owned && booked > maxConflictBooked)
                {
                    maxConflictBooked = booked;
                    conflictDay = d;
                }

                if (d <= demandEnd && booked > maxDemandBooked)
                {
                    maxDemandBooked = booked;
                    demandDay = d;
                }
            }

            var label = string.IsNullOrWhiteSpace(prod.Designation)
                ? prod.Reference
                : $"{prod.Reference} — {prod.Designation}";

            if (conflictDay is not null && conflictCount < MaxPerKind)
            {
                conflictCount++;
                conflictedProducts.Add(pid);
                alerts.Add(new DashboardAlertRow(
                    DashboardAlertKind.AvailabilityConflict,
                    DashboardAlertSeverity.Critical,
                    _locale.T("DashAlert_CatDispo"),
                    _locale.Tf("DashAlert_ConflictTitle", prod.Reference),
                    _locale.Tf(
                        "DashAlert_ConflictDetail",
                        conflictDay.Value.ToString("d", CultureInfo.CurrentCulture),
                        maxConflictBooked.ToString("N0", CultureInfo.CurrentCulture),
                        owned.ToString("N0", CultureInfo.CurrentCulture)),
                    DashboardAlertNav.Availability,
                    pid,
                    label));
            }
            else if (demandDay is not null
                     && maxDemandBooked >= owned * 0.8m
                     && maxDemandBooked <= owned
                     && !conflictedProducts.Contains(pid)
                     && demandCount < MaxPerKind)
            {
                // ≥80% of capacity in the next week, without a hard conflict already listed.
                demandCount++;
                alerts.Add(new DashboardAlertRow(
                    DashboardAlertKind.HighDemandSoon,
                    DashboardAlertSeverity.Warning,
                    _locale.T("DashAlert_CatDispo"),
                    _locale.Tf("DashAlert_HighDemandTitle", prod.Reference),
                    _locale.Tf(
                        "DashAlert_HighDemandDetail",
                        demandDay.Value.ToString("d", CultureInfo.CurrentCulture),
                        maxDemandBooked.ToString("N0", CultureInfo.CurrentCulture),
                        owned.ToString("N0", CultureInfo.CurrentCulture)),
                    DashboardAlertNav.Availability,
                    pid,
                    label));
            }
        }
    }

    private async Task AddReturnConditionAlertsAsync(
        AppDbContext db,
        DateTime today,
        List<DashboardAlertRow> alerts,
        CancellationToken ct)
    {
        var since = today.AddDays(-30);
        var rows = await (
            from ret in db.BonSortieProduitRetours.AsNoTracking()
            join ligne in db.BonSortieProduitLignes.AsNoTracking() on ret.BonSortieProduitLigneId equals ligne.Id
            join b in db.BonsSortie.AsNoTracking() on ligne.BonSortieId equals b.Id
            join p in db.Produits.AsNoTracking() on ligne.ProduitId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where ret.DateRetour >= since
                  && (ret.Etat == BonSortieProduitRetourEtats.Damaged
                      || ret.Etat == BonSortieProduitRetourEtats.ToClean)
            orderby ret.DateRetour descending
            select new
            {
                BonId = b.Id,
                b.Numero,
                ret.Etat,
                ret.Quantite,
                ret.DateRetour,
                Ref = p != null ? p.Reference : "",
                Des = p != null ? p.Designation : ""
            }).Take(MaxPerKind).ToListAsync(ct);

        foreach (var row in rows)
        {
            var etatLabel = row.Etat == BonSortieProduitRetourEtats.Damaged
                ? _locale.T("DashAlert_EtatDamaged")
                : _locale.T("DashAlert_EtatToClean");
            var product = string.IsNullOrWhiteSpace(row.Des)
                ? row.Ref
                : $"{row.Ref} — {row.Des}";

            alerts.Add(new DashboardAlertRow(
                DashboardAlertKind.ReturnConditionAction,
                DashboardAlertSeverity.Warning,
                _locale.T("DashAlert_CatStock"),
                _locale.Tf("DashAlert_ConditionTitle", row.Numero),
                _locale.Tf(
                    "DashAlert_ConditionDetail",
                    product,
                    etatLabel,
                    row.Quantite.ToString("N0", CultureInfo.CurrentCulture),
                    row.DateRetour.ToString("d", CultureInfo.CurrentCulture)),
                DashboardAlertNav.BonSortie,
                row.BonId));
        }
    }

    private async Task AddStockBelowMinAsync(
        AppDbContext db,
        List<DashboardAlertRow> alerts,
        CancellationToken ct)
    {
        var rows = (await db.Produits.AsNoTracking()
            .Where(p => p.Actif && p.StockMinimum > 0 && p.StockActuel < p.StockMinimum)
            .Select(p => new { p.Id, p.Reference, p.Designation, p.StockActuel, p.StockMinimum })
            .ToListAsync(ct))
            .OrderBy(p => p.StockActuel - p.StockMinimum)
            .Take(MaxPerKind)
            .ToList();

        foreach (var p in rows)
        {
            alerts.Add(new DashboardAlertRow(
                DashboardAlertKind.StockBelowMin,
                DashboardAlertSeverity.Warning,
                _locale.T("DashAlert_CatStock"),
                _locale.Tf("DashAlert_StockTitle", p.Reference),
                _locale.Tf(
                    "DashAlert_StockDetail",
                    p.Designation,
                    p.StockActuel.ToString("N2", CultureInfo.CurrentCulture),
                    p.StockMinimum.ToString("N2", CultureInfo.CurrentCulture)),
                DashboardAlertNav.Produits,
                p.Id,
                $"{p.Reference} — {p.Designation}"));
        }
    }

    private async Task AddUnpaidOverdueAsync(
        AppDbContext db,
        DateTime today,
        List<DashboardAlertRow> alerts,
        CancellationToken ct)
    {
        var cfg = await _settings.GetAsync(ct);
        var dev = string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise!;

        var unpaid = await db.Factures.AsNoTracking()
            .Where(f => !f.EstPayee && f.DateEcheance.Date < today)
            .Select(f => new
            {
                f.Id,
                f.Numero,
                f.DateEcheance,
                TTC = f.Lignes.Sum(l => l.Quantite * l.PrixUnitaireHT * (1m - l.Remise / 100m) * (1m + l.TauxTVA / 100m)) * (1m - f.RemiseGlobale / 100m),
                Paye = f.Paiements.Sum(p => (decimal?)p.Montant) ?? 0m
            })
            .OrderBy(f => f.DateEcheance)
            .Take(80)
            .ToListAsync(ct);

        var count = 0;
        foreach (var f in unpaid)
        {
            var reste = f.TTC - f.Paye;
            if (reste <= 0.01m) continue;
            if (count >= MaxPerKind) break;
            count++;

            var days = (today - f.DateEcheance.Date).Days;
            alerts.Add(new DashboardAlertRow(
                DashboardAlertKind.UnpaidInvoice,
                DashboardAlertSeverity.Critical,
                _locale.T("DashAlert_CatMoney"),
                _locale.Tf("DashAlert_UnpaidTitle", f.Numero),
                _locale.Tf(
                    "DashAlert_UnpaidDetail",
                    CurrencyHelper.Format(reste, dev),
                    days.ToString(CultureInfo.CurrentCulture)),
                DashboardAlertNav.Facture,
                f.Id));
        }
    }
}
