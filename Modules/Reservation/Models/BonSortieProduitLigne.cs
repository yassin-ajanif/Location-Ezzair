using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class BonSortieProduitLigne : BaseEntity
{
    public int BonSortieId { get; set; }
    public BonSortie? BonSortie { get; set; }
    public int? ProduitId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    /// <summary>Sum of <see cref="Retours"/>; kept denormalized for stock/status/Etat client.</summary>
    public decimal QuantiteRetournee { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    public string Note { get; set; } = string.Empty;
    public ICollection<BonSortieProduitRetour> Retours { get; set; } = new List<BonSortieProduitRetour>();
}
