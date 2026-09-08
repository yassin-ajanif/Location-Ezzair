using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class SoftReservationProduitLineRow : ObservableObject
{
    private static readonly IBrush GoldBg = Brush.Parse("#F5E9C8");
    private static readonly IBrush GoldBorder = Brush.Parse("#C4A035");
    private static readonly IBrush GoldFg = Brush.Parse("#8A7020");

    [ObservableProperty] private int? _produitId;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private decimal _quantite;
    [ObservableProperty] private decimal _prixUnitaireHt;
    [ObservableProperty] private decimal _remise;
    [ObservableProperty] private decimal _tauxTva;
    [ObservableProperty] private string _note = string.Empty;

    public decimal MontantHt => DocumentTotalsHelper.LigneHT(Quantite, PrixUnitaireHt, Remise);

    public decimal MontantTtc => MontantHt * (1 + TauxTva / 100m);

    public IBrush QteBackground => GoldBg;
    public IBrush QteBorder => GoldBorder;
    public IBrush QteForeground => GoldFg;

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();
    partial void OnRemiseChanged(decimal value) => NotifyMontants();
    partial void OnTauxTvaChanged(decimal value) => NotifyMontants();

    public void ApplyCatalogProduct(Produit p)
    {
        ProduitId = p.Id;
        Reference = p.Reference;
        Designation = p.Designation;
        PrixUnitaireHt = p.PrixLocationHT > 0 ? p.PrixLocationHT : p.PrixVenteHT;
        TauxTva = p.TauxTVA;
        NotifyMontants();
    }

    public void ApplyCatalogItem(DocumentCatalogItem item)
    {
        ProduitId = item.Id;
        PrixUnitaireHt = item.PrixLocationHT > 0 ? item.PrixLocationHT : item.PrixVenteHT;
        Reference = item.Reference;
        Designation = item.Designation;
        TauxTva = item.TauxTVA;
        NotifyMontants();
    }

    private void NotifyMontants()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
    }
}
