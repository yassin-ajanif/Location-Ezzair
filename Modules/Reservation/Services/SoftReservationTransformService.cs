using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reservation.Services;

public interface ISoftReservationTransformService
{
    /// <summary>Creates a BonSortie from a soft Reservation, applies stock, links both sides. Returns BonSortie id.</summary>
    Task<int> TransformToBonSortieAsync(int reservationId, int? userId, CancellationToken cancellationToken = default);
}

public sealed class SoftReservationTransformService : ISoftReservationTransformService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IReservationWorkflowService _workflow;

    public SoftReservationTransformService(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IReservationWorkflowService workflow)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _workflow = workflow;
    }

    public async Task<int> TransformToBonSortieAsync(int reservationId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var res = await db.Reservations
            .Include(r => r.ProduitLignes)
            .Include(r => r.ServiceLignes)
            .FirstAsync(r => r.Id == reservationId, cancellationToken);
if (res.BonSortieId is { } existingId)
        {
            var exists = await db.BonsSortie.AsNoTracking().AnyAsync(b => b.Id == existingId, cancellationToken);
            if (exists)
                return existingId;
            res.BonSortieId = null;
        }

        if (res.ClientId == 0 || (res.ProduitLignes.Count == 0 && res.ServiceLignes.Count == 0))
            throw new InvalidOperationException("Client and at least one line are required.");

        var bsNumero = await _numbers.NextLocationAsync(cancellationToken);
        var bs = new BonSortie
        {
            Numero = bsNumero,
            ClientId = res.ClientId,
            Date = DateTime.Today,
            DateDebut = res.DateDebut,
            DateFinPrevue = res.DateFinPrevue,
            Statut = StatutBonSortie.EnCours,
            Caution = res.Caution,
            RemiseGlobale = res.RemiseGlobale,
            Note = res.Note ?? string.Empty,
            ReservationId = res.Id,
            CreatedByUserId = userId
        };

        foreach (var l in res.ProduitLignes.OrderBy(x => x.Id))
        {
            bs.ProduitLignes.Add(new BonSortieProduitLigne
            {
                ProduitId = l.ProduitId,
                Designation = l.Designation,
                Quantite = l.Quantite,
                QuantiteRetournee = 0,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA,
                Note = l.Note ?? string.Empty,
                CreatedByUserId = userId
            });
        }

        foreach (var l in res.ServiceLignes.OrderBy(x => x.Id))
        {
            bs.ServiceLignes.Add(new BonSortieServiceLigne
            {
                ServiceId = l.ServiceId,
                Designation = l.Designation,
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA,
                Note = l.Note ?? string.Empty,
                CreatedByUserId = userId
            });
        }

        db.BonsSortie.Add(bs);
        await db.SaveChangesAsync(cancellationToken);

        res.BonSortieId = bs.Id;
        res.Statut = StatutReservation.Transformee;
        await db.SaveChangesAsync(cancellationToken);

        await _workflow.ResyncStockAsync(bs.Id, userId, cancellationToken);
        return bs.Id;
    }
}
