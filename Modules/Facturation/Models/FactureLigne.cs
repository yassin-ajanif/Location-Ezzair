using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

public class FactureLigne : BaseEntity
{
    public int FactureId { get; set; }
    public Facture? Facture { get; set; }
    public int? BonSortieId { get; set; }
    public BonSortie? BonSortie { get; set; }
    public int? ProduitId { get; set; }
    public int? ServiceId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    /// <summary>Snapshot when line comes from a day-billed location product.</summary>
    public bool RentedByDay { get; set; }
    /// <summary>Billing days when <see cref="RentedByDay"/>; otherwise null.</summary>
    public int? Days { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    /// <summary>Unit / packaging label (e.g. carton, pièce).</summary>
    public string Conditionnement { get; set; } = string.Empty;
}
