using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class BonSortieServiceLigne : BaseEntity
{
    public int BonSortieId { get; set; }
    public BonSortie? BonSortie { get; set; }
    public int? ServiceId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    public string Note { get; set; } = string.Empty;
}
