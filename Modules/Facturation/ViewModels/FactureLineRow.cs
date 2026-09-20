using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Services.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Facturation.ViewModels;

public partial class FactureLineRow : ObservableObject
{
    [ObservableProperty] private int? _bonSortieId;
    [ObservableProperty] private int? _produitId;
    [ObservableProperty] private int? _serviceId;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private string _conditionnement = string.Empty;
    [ObservableProperty] private decimal _quantite = 1;
    [ObservableProperty] private decimal _prixUnitaireHt;
    [ObservableProperty] private bool _rentedByDay;
    [ObservableProperty] private int? _days;
    [ObservableProperty] private decimal _remise;
    [ObservableProperty] private decimal _tauxTva;

    public bool IsService => ServiceId is > 0;

    public decimal MontantHt => DocumentTotalsHelper.LigneHT(Quantite, PrixUnitaireHt, Remise, RentedByDay, Days);

    public decimal MontantTtc => MontantHt * (1 + TauxTva / 100m);

    public decimal PrixUnitaireTtc
    {
        get => DocumentTotalsHelper.PrixUnitaireTtc(PrixUnitaireHt, TauxTva);
        set
        {
            var ht = DocumentTotalsHelper.PrixUnitaireHtFromTtc(value, TauxTva);
            if (PrixUnitaireHt == ht)
                OnPropertyChanged(nameof(PrixUnitaireTtc));
            else
                PrixUnitaireHt = ht;
        }
    }

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();
    partial void OnRentedByDayChanged(bool value) => NotifyMontants();
    partial void OnDaysChanged(int? value) => NotifyMontants();
    partial void OnRemiseChanged(decimal value) => NotifyMontants();
    partial void OnTauxTvaChanged(decimal value) => NotifyMontants();

    public void ApplyCatalogProduct(Produit p)
    {
        ProduitId = p.Id;
        ServiceId = null;
        Reference = p.Reference;
        Designation = p.Designation;
        Conditionnement = p.Unite;
        PrixUnitaireHt = p.PrixVenteHT;
        TauxTva = p.TauxTVA;
        RentedByDay = false;
        Days = null;
        NotifyMontants();
    }

    public void ApplyCatalogService(Service s)
    {
        ServiceId = s.Id;
        ProduitId = null;
        Reference = s.Reference;
        Designation = s.Designation;
        Conditionnement = s.Unite;
        PrixUnitaireHt = s.PrixVenteHT;
        TauxTva = s.TauxTVA;
        RentedByDay = false;
        Days = null;
        NotifyMontants();
    }

    public void ApplyCatalogItem(DocumentCatalogItem item)
    {
        if (item.Kind == DocumentCatalogKind.Service)
        {
            ServiceId = item.Id;
            ProduitId = null;
            RentedByDay = false;
            Days = null;
        }
        else
        {
            ProduitId = item.Id;
            ServiceId = null;
            // Direct sale on facture stays flat; day billing comes from bon de sortie.
            RentedByDay = false;
            Days = null;
        }

        Reference = item.Reference;
        Designation = item.Designation;
        Conditionnement = item.Unite;
        PrixUnitaireHt = item.PrixVenteHT;
        TauxTva = item.TauxTVA;
        NotifyMontants();
    }

    private void NotifyMontants()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
        OnPropertyChanged(nameof(PrixUnitaireTtc));
    }
}
