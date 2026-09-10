using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

/// <summary>Soft booking: holds capacity for a period without reducing stock.</summary>
public class Reservation : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFinPrevue { get; set; }
    public StatutReservation Statut { get; set; } = StatutReservation.Confirmee;
    public decimal Caution { get; set; }
    public decimal RemiseGlobale { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<ReservationProduitLigne> ProduitLignes { get; set; } = [];
    public List<ReservationServiceLigne> ServiceLignes { get; set; } = [];
}
