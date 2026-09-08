using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class BonSortieProduitRetour : BaseEntity
{
    public int BonSortieProduitLigneId { get; set; }
    public BonSortieProduitLigne? BonSortieProduitLigne { get; set; }
    public DateTime DateRetour { get; set; }
    public decimal Quantite { get; set; }
    /// <summary>Condition on return: <see cref="BonSortieProduitRetourEtats"/>.</summary>
    public string Etat { get; set; } = BonSortieProduitRetourEtats.Good;
    public string Note { get; set; } = string.Empty;
}
