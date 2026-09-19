using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class ReservationProduitLigne : BaseEntity
{
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public int? ProduitId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    /// <summary>Snapshot from product at line creation; drives day-based billing.</summary>
    public bool RentedByDay { get; set; }
    /// <summary>Billing days when <see cref="RentedByDay"/>; otherwise null.</summary>
    public int? Days { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    public string Note { get; set; } = string.Empty;
}
