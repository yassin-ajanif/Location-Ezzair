using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reservation.Services;

public sealed class ReservationAvailabilityService : IReservationAvailabilityService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReservationAvailabilityService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<ReservationAvailabilityConflict>> CheckAsync(
        int? excludeBonSortieId,
        int? excludeSoftReservationId,
        DateTime dateDebut,
        DateTime dateFin,
        IEnumerable<ReservationAvailabilityLineRequest> lines,
        CancellationToken cancellationToken = default)
    {
        var periodStart = dateDebut.Date;
        var periodEnd = dateFin.Date;
        if (periodEnd < periodStart)
            (periodStart, periodEnd) = (periodEnd, periodStart);

        var requested = lines
            .Where(l => l.ProduitId > 0)
            .GroupBy(l => l.ProduitId)
            .Select(g => (
                ProduitId: g.Key,
                Demande: g.Sum(x => Math.Max(0m, x.Quantite - x.QuantiteRetournee))))
            .Where(x => x.Demande > 0)
            .ToList();

        if (requested.Count == 0)
            return [];

        var produitIds = requested.Select(x => x.ProduitId).ToList();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var produits = await db.Produits.AsNoTracking()
            .Where(p => produitIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Reference, p.Designation, p.StockActuel })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var allOpenBsLines = await db.BonSortieProduitLignes.AsNoTracking()
            .Where(l => l.ProduitId != null && produitIds.Contains(l.ProduitId.Value))
            .Where(l => l.Quantite > l.QuantiteRetournee)
            .Select(l => new
            {
                BonSortieId = l.BonSortieId,
                ProduitId = l.ProduitId!.Value,
                Encore = l.Quantite - l.QuantiteRetournee
            })
            .ToListAsync(cancellationToken);

        var ownedByProduit = new Dictionary<int, decimal>();
        foreach (var pid in produitIds)
        {
            var stock = produits.TryGetValue(pid, out var p) ? p.StockActuel : 0m;
            var outQty = allOpenBsLines.Where(l => l.ProduitId == pid).Sum(l => l.Encore);
            ownedByProduit[pid] = stock + outQty;
        }

        // Only bons that still have qty out can block stock. While qty remains out,
        // occupancy must not end on DateFinPrevue alone (overdue / late return).
        var openBsIds = allOpenBsLines.Select(l => l.BonSortieId).Distinct().ToList();
        var openBsHeaders = openBsIds.Count == 0
            ? []
            : await db.BonsSortie.AsNoTracking()
                .Where(r => openBsIds.Contains(r.Id))
                .Where(r => excludeBonSortieId == null || r.Id != excludeBonSortieId.Value)
                .Select(r => new
                {
                    r.Id,
                    r.Numero,
                    r.ClientId,
                    r.DateDebut,
                    r.DateFinPrevue,
                    r.DateRetourEffective
                })
                .ToListAsync(cancellationToken);

        var overlappingBs = openBsHeaders
            .Select(r => new
            {
                r.Id,
                r.Numero,
                r.ClientId,
                DateDebut = r.DateDebut.Date,
                DateFin = OccupancyEndWhileStillOut(),
                DateFinDisplay = OccupancyDisplayEndWhileStillOut(r.DateRetourEffective, r.DateFinPrevue)
            })
            .Where(r => PeriodsOverlap(periodStart, periodEnd, r.DateDebut, r.DateFin))
            .ToList();

        var overlapBsIds = overlappingBs.Select(r => r.Id).ToHashSet();
        var bsById = overlappingBs.ToDictionary(r => r.Id);
        var bsOpenOnOverlap = allOpenBsLines
            .Where(l => overlapBsIds.Contains(l.BonSortieId))
            .ToList();

        var softHeaders = await db.Reservations.AsNoTracking()
            .Where(r => r.Statut == StatutReservation.Confirmee)
            .Where(r => excludeSoftReservationId == null || r.Id != excludeSoftReservationId.Value)
            .Select(r => new
            {
                r.Id,
                r.Numero,
                r.ClientId,
                r.DateDebut,
                DateFin = r.DateFinPrevue
            })
            .ToListAsync(cancellationToken);

        softHeaders = softHeaders
            .Where(r => PeriodsOverlap(periodStart, periodEnd, r.DateDebut.Date, r.DateFin.Date))
            .ToList();

        var softOverlapIds = softHeaders.Select(r => r.Id).ToHashSet();
        var softById = softHeaders.ToDictionary(r => r.Id);

        var softLinesOnOverlap = softOverlapIds.Count == 0
            ? new List<(int SoftReservationId, int ProduitId, decimal Encore)>()
            : (await db.ReservationProduitLignes.AsNoTracking()
                .Where(l => softOverlapIds.Contains(l.ReservationId))
                .Where(l => l.ProduitId != null && produitIds.Contains(l.ProduitId.Value))
                .Where(l => l.Quantite > 0)
                .Select(l => new { SoftReservationId = l.ReservationId, ProduitId = l.ProduitId!.Value, Encore = l.Quantite })
                .ToListAsync(cancellationToken))
            .Select(l => (l.SoftReservationId, l.ProduitId, l.Encore))
            .ToList();

        var clientIds = overlappingBs.Select(r => r.ClientId)
            .Concat(softHeaders.Select(r => r.ClientId))
            .Distinct()
            .ToList();
        var clientNames = clientIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Tiers.AsNoTracking()
                .Where(t => clientIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Nom, cancellationToken);

        var conflicts = new List<ReservationAvailabilityConflict>();
        foreach (var req in requested)
        {
            var dejaBs = bsOpenOnOverlap.Where(l => l.ProduitId == req.ProduitId).Sum(l => l.Encore);
            var dejaSoft = softLinesOnOverlap.Where(l => l.ProduitId == req.ProduitId).Sum(l => l.Encore);
            var deja = dejaBs + dejaSoft;
            var owned = ownedByProduit.GetValueOrDefault(req.ProduitId);
            var disponible = owned - deja;
            if (req.Demande <= disponible)
                continue;

            produits.TryGetValue(req.ProduitId, out var prod);

            var sources = new List<ReservationAvailabilityConflictSource>();
            sources.AddRange(bsOpenOnOverlap
                .Where(l => l.ProduitId == req.ProduitId)
                .GroupBy(l => l.BonSortieId)
                .Select(g =>
                {
                    var res = bsById[g.Key];
                    clientNames.TryGetValue(res.ClientId, out var nom);
                    return new ReservationAvailabilityConflictSource(
                        res.Numero,
                        string.IsNullOrWhiteSpace(nom) ? $"#{res.ClientId}" : nom,
                        res.DateDebut,
                        res.DateFinDisplay,
                        g.Sum(x => x.Encore));
                }));
            sources.AddRange(softLinesOnOverlap
                .Where(l => l.ProduitId == req.ProduitId)
                .GroupBy(l => l.SoftReservationId)
                .Select(g =>
                {
                    var res = softById[g.Key];
                    clientNames.TryGetValue(res.ClientId, out var nom);
                    return new ReservationAvailabilityConflictSource(
                        res.Numero,
                        string.IsNullOrWhiteSpace(nom) ? $"#{res.ClientId}" : nom,
                        res.DateDebut.Date,
                        res.DateFin.Date,
                        g.Sum(x => x.Encore));
                }));

            sources = sources.OrderBy(s => s.DateDebut).ThenBy(s => s.Numero).ToList();

            conflicts.Add(new ReservationAvailabilityConflict(
                req.ProduitId,
                prod?.Reference ?? string.Empty,
                prod?.Designation ?? string.Empty,
                req.Demande,
                Math.Max(0, disponible),
                owned,
                deja,
                sources));
        }

        return conflicts;
    }

    public async Task<ProductAvailabilityMonthResult?> GetProductMonthAsync(
        int produitId,
        DateTime month,
        decimal qtyNeeded,
        CancellationToken cancellationToken = default)
    {
        if (produitId <= 0)
            return null;

        var needed = Math.Max(1m, qtyNeeded);
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // Grid includes days from previous/next month to fill weeks (Monday-first).
        var gridStart = monthStart.AddDays(-(((int)monthStart.DayOfWeek + 6) % 7));
        var gridEnd = gridStart.AddDays(41); // 6 weeks

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var produit = await db.Produits.AsNoTracking()
            .Where(p => p.Id == produitId)
            .Select(p => new { p.Id, p.Reference, p.Designation, p.StockActuel })
            .FirstOrDefaultAsync(cancellationToken);
        if (produit is null)
            return null;

        var openLinesRaw = await (
            from l in db.BonSortieProduitLignes.AsNoTracking()
            join r in db.BonsSortie.AsNoTracking() on l.BonSortieId equals r.Id
            where l.ProduitId == produitId && l.Quantite > l.QuantiteRetournee
            select new
            {
                r.Id,
                r.Numero,
                r.ClientId,
                r.DateDebut,
                r.DateFinPrevue,
                r.DateRetourEffective,
                Encore = l.Quantite - l.QuantiteRetournee
            }).ToListAsync(cancellationToken);

        var openLines = openLinesRaw
            .Select(r => new
            {
                r.Id,
                r.Numero,
                r.ClientId,
                DateDebut = r.DateDebut.Date,
                DateFin = OccupancyEndWhileStillOut(),
                DateFinDisplay = OccupancyDisplayEndWhileStillOut(r.DateRetourEffective, r.DateFinPrevue),
                r.Encore,
                IsSoft = false
            })
            .ToList();

        var softLines = await (
            from l in db.ReservationProduitLignes.AsNoTracking()
            join r in db.Reservations.AsNoTracking() on l.ReservationId equals r.Id
            where r.Statut == StatutReservation.Confirmee
                  && l.ProduitId == produitId
                  && l.Quantite > 0
            select new
            {
                r.Id,
                r.Numero,
                r.ClientId,
                DateDebut = r.DateDebut.Date,
                DateFin = r.DateFinPrevue.Date,
                DateFinDisplay = r.DateFinPrevue.Date,
                Encore = l.Quantite,
                IsSoft = true
            }).ToListAsync(cancellationToken);

        var owned = produit.StockActuel + openLines.Sum(l => l.Encore);

        var relevant = openLines
            .Concat(softLines)
            .Where(l => PeriodsOverlap(gridStart, gridEnd, l.DateDebut, l.DateFin))
            .ToList();

        var clientIds = relevant.Select(l => l.ClientId).Distinct().ToList();
        var clientNames = clientIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Tiers.AsNoTracking()
                .Where(t => clientIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Nom, cancellationToken);

        var softByDay = new Dictionary<DateTime, decimal>();
        var bsByDay = new Dictionary<DateTime, decimal>();
        foreach (var b in relevant)
        {
            var start = b.DateDebut;
            var end = b.DateFin;
            if (end < start) (start, end) = (end, start);
            // Open-ended overdue bons use MaxValue; never walk past the visible grid.
            if (end > gridEnd) end = gridEnd;
            if (start < gridStart) start = gridStart;
            var target = b.IsSoft ? softByDay : bsByDay;
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                target.TryGetValue(d, out var sum);
                target[d] = sum + b.Encore;
            }
        }

        var days = new List<ProductAvailabilityDay>(42);
        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var inMonth = date.Month == monthStart.Month;
            softByDay.TryGetValue(date, out var softBooked);
            bsByDay.TryGetValue(date, out var bsBooked);
            var booked = softBooked + bsBooked;
            var available = Math.Max(0, owned - booked);
            ProductAvailabilityDayLevel level;
            if (!inMonth)
                level = ProductAvailabilityDayLevel.OutsideMonth;
            else if (available >= needed && booked <= 0)
                level = ProductAvailabilityDayLevel.Free;
            else if (available >= needed)
                level = ProductAvailabilityDayLevel.Partial;
            else
                level = ProductAvailabilityDayLevel.Full;

            days.Add(new ProductAvailabilityDay(
                date, inMonth, booked, softBooked, bsBooked, available, owned, level));
        }

        var today = DateTime.Today;
        var upcomingBookings = relevant
            .Where(l => l.DateFin >= today)
            .GroupBy(l => (l.IsSoft, l.Id, l.Numero))
            .Select(g =>
            {
                var first = g.First();
                clientNames.TryGetValue(first.ClientId, out var nom);
                return new ProductAvailabilityBooking(
                    first.Numero,
                    string.IsNullOrWhiteSpace(nom) ? $"#{first.ClientId}" : nom,
                    first.DateDebut,
                    first.DateFinDisplay,
                    g.Sum(x => x.Encore));
            })
            .OrderBy(b => b.DateDebut)
            .ThenBy(b => b.Numero)
            .ToList();

        var freeWindows = BuildFreeWindows(
            days.Where(d => d.IsCurrentMonth && d.Date >= today).OrderBy(d => d.Date),
            needed);

        return new ProductAvailabilityMonthResult(
            produit.Id,
            produit.Reference,
            produit.Designation,
            owned,
            monthStart,
            days,
            upcomingBookings,
            freeWindows);
    }

    private static List<ProductAvailabilityFreeWindow> BuildFreeWindows(
        IEnumerable<ProductAvailabilityDay> futureDays,
        decimal needed)
    {
        var windows = new List<ProductAvailabilityFreeWindow>();
        DateTime? start = null;
        DateTime? end = null;
        decimal minAvail = 0;

        foreach (var day in futureDays)
        {
            var ok = day.Available >= needed;
            if (ok)
            {
                if (start is null)
                {
                    start = day.Date;
                    end = day.Date;
                    minAvail = day.Available;
                }
                else
                {
                    end = day.Date;
                    minAvail = Math.Min(minAvail, day.Available);
                }
            }
            else if (start is not null && end is not null)
            {
                windows.Add(new ProductAvailabilityFreeWindow(start.Value, end.Value, minAvail));
                start = end = null;
            }
        }

        if (start is not null && end is not null)
            windows.Add(new ProductAvailabilityFreeWindow(start.Value, end.Value, minAvail));

        return windows;
    }

    private static bool PeriodsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) =>
        aStart <= bEnd && bStart <= aEnd;

    /// <summary>
    /// Stock hold end for a bon de sortie line that still has quantity out.
    /// Planned end alone must not free the item; stay open-ended until returned.
    /// Callers clamp this to their calendar / check window.
    /// </summary>
    private static DateTime OccupancyEndWhileStillOut() => DateTime.MaxValue.Date;

    /// <summary>
    /// UI-friendly end date while qty is still out (avoid showing MaxValue).
    /// </summary>
    private static DateTime OccupancyDisplayEndWhileStillOut(DateTime? dateRetourEffective, DateTime dateFinPrevue)
    {
        if (dateRetourEffective is DateTime ret)
            return ret.Date;

        var planned = dateFinPrevue.Date;
        var today = DateTime.Today;
        return planned >= today ? planned : today;
    }
}
