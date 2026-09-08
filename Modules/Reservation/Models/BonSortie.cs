using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

/// <summary>Stock-affecting exit document (bon de sortie). Stock movements live on this document, not on soft-booking reservations.</summary>
public class BonSortie : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFinPrevue { get; set; }
    public DateTime? DateRetourEffective { get; set; }
    public StatutBonSortie Statut { get; set; } = StatutBonSortie.EnCours;
    public decimal Caution { get; set; }
    public decimal RemiseGlobale { get; set; }
    public string Note { get; set; } = string.Empty;
    public int? FactureId { get; set; }
    public Facture? Facture { get; set; }
    /// <summary>Linked delivery note (Vers BL). Stock stays on BonSortie.</summary>
    public int? BonLivraisonId { get; set; }
    public BonLivraison? BonLivraison { get; set; }
    /// <summary>Origin soft-booking reservation, if created via Vers bon de sortie.</summary>
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public List<BonSortieProduitLigne> ProduitLignes { get; set; } = [];
    public List<BonSortieServiceLigne> ServiceLignes { get; set; } = [];
}
