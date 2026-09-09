using System.Globalization;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reporting.Services;

public sealed class DashboardAlertsService : IDashboardAlertsService
{
    private const int MaxPerKind = 15;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILocaleService _locale;

    public DashboardAlertsService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILocaleService locale)
    {
        _dbFactory = dbFactory;
        _locale = locale;
    }

    public async Task<IReadOnlyList<DashboardAlertRow>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var alerts = new List<DashboardAlertRow>();

        await AddBonSortieReturnAlertsAsync(db, today, alerts, cancellationToken);
        await AddExpiredSoftReservationsAsync(db, today, alerts, cancellationToken);

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

        foreach (var row in open.OrderBy(x => x.DateFinPrevue))
        {
            var fin = row.DateFinPrevue.Date;
            var daysLate = (today - fin).Days;
            if (daysLate < 1) continue;
            if (overdueCount >= MaxPerKind) continue;

            overdueCount++;
            var client = string.IsNullOrWhiteSpace(row.Client) ? $"#{row.Id}" : row.Client;
            var qty = row.Encore.ToString("N0", CultureInfo.CurrentCulture);
            var days = daysLate.ToString(CultureInfo.CurrentCulture);
            alerts.Add(new DashboardAlertRow(
                DashboardAlertKind.ReturnOverdue,
                DashboardAlertSeverity.Critical,
                _locale.T("DashAlert_CatOps"),
                _locale.Tf("DashAlert_OverdueTitle", row.Numero),
                _locale.Tf("DashAlert_OverdueDetailBefore", client),
                _locale.Tf("DashAlert_DaysFmt", days),
                _locale.Tf("DashAlert_OverdueDetailAfter", qty),
                DashboardAlertNav.BonSortie,
                row.Id));
        }
    }

    private async Task AddExpiredSoftReservationsAsync(
        AppDbContext db,
        DateTime today,
        List<DashboardAlertRow> alerts,
        CancellationToken ct)
    {
        var rows = await (
            from r in db.Reservations.AsNoTracking()
            join t in db.Tiers.AsNoTracking() on r.ClientId equals t.Id
            where r.Statut == StatutReservation.Confirmee
                  && r.DateFinPrevue.Date < today
            orderby r.DateFinPrevue
            select new { r.Id, r.Numero, r.DateFinPrevue, Client = t.Nom }
        ).Take(MaxPerKind).ToListAsync(ct);

        foreach (var row in rows)
        {
            var client = string.IsNullOrWhiteSpace(row.Client) ? $"#{row.Id}" : row.Client;
            var days = (today - row.DateFinPrevue.Date).Days.ToString(CultureInfo.CurrentCulture);
            alerts.Add(new DashboardAlertRow(
                DashboardAlertKind.SoftReservationExpired,
                DashboardAlertSeverity.Warning,
                _locale.T("DashAlert_CatOps"),
                _locale.Tf("DashAlert_SoftExpiredTitle", row.Numero),
                _locale.Tf("DashAlert_SoftExpiredDetailBefore", client),
                _locale.Tf("DashAlert_DaysFmt", days),
                string.Empty,
                DashboardAlertNav.SoftReservation,
                row.Id));
        }
    }
}
