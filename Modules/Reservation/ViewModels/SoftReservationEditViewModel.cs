using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Modules.Reservation.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TiersEntity = GestionCommerciale.Modules.Tiers.Models.Tiers;
using TypeTiers = GestionCommerciale.Modules.Tiers.Models.TypeTiers;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public sealed class SoftReservationStatutOption
{
    public required StatutReservation Value { get; init; }
    public required string Label { get; init; }
}

public partial class SoftReservationEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;
    private readonly IReservationAvailabilityService _availability;
    private readonly ISoftReservationTransformService _transform;
    private readonly AddLineCatalogSearchCoordinator _addLineSearch;

    public SoftReservationEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IAppSettingsService settings,
        ICatalogSearchService catalogSearch,
        IReservationAvailabilityService availability,
        ISoftReservationTransformService transform)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _settings = settings;
        _availability = availability;
        _transform = transform;
        _addLineSearch = new AddLineCatalogSearchCoordinator(catalogSearch);
        _locale.CultureApplied += (_, _) => RefreshUi();
        ProduitLignes.CollectionChanged += ProduitLignesOnCollectionChanged;
        ServiceLignes.CollectionChanged += ServiceLignesOnCollectionChanged;
        Title = _locale.T("SoftRes_Title");
        RefreshUi();
    }

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnToBonSortie = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;
    [ObservableProperty] private string _lblClient = string.Empty;
    [ObservableProperty] private string _wmClientSearch = string.Empty;
    [ObservableProperty] private string _lblDate = string.Empty;
    [ObservableProperty] private string _lblDateDebut = string.Empty;
    [ObservableProperty] private string _lblDateFin = string.Empty;
    [ObservableProperty] private string _lblStatut = string.Empty;
    [ObservableProperty] private string _lblCaution = string.Empty;
    [ObservableProperty] private string _lblRemise = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;
    [ObservableProperty] private string _btnRemoveLine = string.Empty;
    [ObservableProperty] private string _lblAddProduct = string.Empty;
    [ObservableProperty] private string _wmAddProduct = string.Empty;
    [ObservableProperty] private string _lblTotals = string.Empty;
    [ObservableProperty] private string _lblProduitsSection = string.Empty;
    [ObservableProperty] private string _lblServicesSection = string.Empty;
    [ObservableProperty] private string _lblDocColRef = string.Empty;
    [ObservableProperty] private string _lblDocColDesignation = string.Empty;
    [ObservableProperty] private string _lblDocColQte = string.Empty;
    [ObservableProperty] private string _lblDocColQteService = string.Empty;
    [ObservableProperty] private string _lblDocColPuHt = string.Empty;
    [ObservableProperty] private string _lblDocColRemise = string.Empty;
    [ObservableProperty] private string _lblDocColTva = string.Empty;
    [ObservableProperty] private string _lblDocColMontantHt = string.Empty;
    [ObservableProperty] private string _lblDocColMontantTtc = string.Empty;

    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;
    public AutoCompleteFilterPredicate<object?> CatalogAutocompleteFilter => DocumentCatalogAutoComplete.ItemFilter;

    public ObservableCollection<DocumentCatalogItem> AddLineSearchResults => _addLineSearch.Results;

    [ObservableProperty] private decimal _totalHt;
    [ObservableProperty] private decimal _totalTva;
    [ObservableProperty] private decimal _totalTtc;
    [ObservableProperty] private string _totalHtLabel = "HT 0,00";
    [ObservableProperty] private string _totalTvaLabel = "TVA 0,00";
    [ObservableProperty] private string _totalTtcLabel = "TTC 0,00";
    [ObservableProperty] private string _devise = string.Empty;
    [ObservableProperty] private string _addLineSearchText = string.Empty;
    [ObservableProperty] private object? _addLineCatalogPick;
    private bool _suppressAddLinePick;

    private void ProduitLignesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (SoftReservationProduitLineRow row in e.NewItems)
                row.PropertyChanged += ProduitLineOnPropertyChanged;
        if (e.OldItems != null)
            foreach (SoftReservationProduitLineRow row in e.OldItems)
                row.PropertyChanged -= ProduitLineOnPropertyChanged;
        RefreshTotals();
    }

    private void ServiceLignesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (SoftReservationServiceLineRow row in e.NewItems)
                row.PropertyChanged += ServiceLineOnPropertyChanged;
        if (e.OldItems != null)
            foreach (SoftReservationServiceLineRow row in e.OldItems)
                row.PropertyChanged -= ServiceLineOnPropertyChanged;
        RefreshTotals();
    }

    private void ProduitLineOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SoftReservationProduitLineRow.ProduitId)
            && sender is SoftReservationProduitLineRow row && row.ProduitId is > 0)
            ConsolidateDuplicateProductLines();
        RefreshTotals();
    }

    private void ServiceLineOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SoftReservationServiceLineRow.ServiceId)
            && sender is SoftReservationServiceLineRow row && row.ServiceId is > 0)
            ConsolidateDuplicateServiceLines();
        RefreshTotals();
    }

    partial void OnAddLineSearchTextChanged(string value)
    {
        if (_suppressAddLinePick) return;
        _addLineSearch.QueueSearch(value);
    }

    private void RefreshUi()
    {
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        BtnToBonSortie = _locale.T("SoftRes_BtnToBonSortie");
        MenuDelete = _locale.T("SoftRes_MenuDelete");
        LblClient = _locale.T("Lbl_Client");
        WmClientSearch = _locale.T("Wm_SearchClient");
        LblDate = _locale.T("Loc_LblDate");
        LblDateDebut = _locale.T("Loc_LblDateDebut");
        LblDateFin = _locale.T("Loc_LblDateFin");
        LblStatut = _locale.T("Loc_ColStatut");
        LblCaution = _locale.T("Loc_LblCaution");
        LblRemise = _locale.T("Lbl_RemisePct");
        LblNote = _locale.T("DevisList_ColNote");
        BtnRemoveLine = _locale.T("Btn_RemoveLine");
        LblAddProduct = _locale.T("Devis_LblAddProduct");
        WmAddProduct = _locale.T("Wm_SearchCatalog");
        LblTotals = _locale.T("Lbl_Totals");
        LblProduitsSection = _locale.T("Res_SectionProduits");
        LblServicesSection = _locale.T("Res_SectionServices");
        LblDocColRef = _locale.T("DocLine_ColRef");
        LblDocColDesignation = _locale.T("DocLine_ColDesignation");
        LblDocColQte = _locale.T("Loc_ColQteLouee");
        LblDocColQteService = _locale.T("Loc_ColQteVendu");
        LblDocColPuHt = _locale.T("DocLine_ColPuHt");
        LblDocColRemise = _locale.T("DocLine_ColRemise");
        LblDocColTva = _locale.T("DocLine_ColTva");
        LblDocColMontantHt = _locale.T("DocLine_ColMontantHt");
        LblDocColMontantTtc = _locale.T("DocLine_ColMontantTtc");
        RefreshStatutOptions();
        NotifyStatutChip();
        UpdateTotalLabels(TotalHt, TotalTva, TotalTtc);
    }

    public ObservableCollection<TiersEntity> Clients { get; } = [];
    public ObservableCollection<SoftReservationProduitLineRow> ProduitLignes { get; } = [];
    public ObservableCollection<SoftReservationServiceLineRow> ServiceLignes { get; } = [];
    public ObservableCollection<SoftReservationStatutOption> StatutOptions { get; } = [];

    [ObservableProperty] private int? _reservationId;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private TiersEntity? _selectedClient;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private DateTime _dateDebut = DateTime.Today;
    [ObservableProperty] private DateTime _dateFinPrevue = DateTime.Today.AddDays(1);
    [ObservableProperty] private StatutReservation _statut = StatutReservation.Confirmee;
    [ObservableProperty] private SoftReservationStatutOption? _selectedStatutOption;
    [ObservableProperty] private string _statutLabel = string.Empty;
    [ObservableProperty] private IBrush _statutChipBackground = Brushes.Transparent;
    [ObservableProperty] private IBrush _statutChipForeground = Brushes.Black;
    [ObservableProperty] private IBrush _statutChipBorder = Brushes.Transparent;
    [ObservableProperty] private decimal _caution;
    [ObservableProperty] private decimal _remiseGlobale;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private SoftReservationProduitLineRow? _selectedProduitLine;
    [ObservableProperty] private SoftReservationServiceLineRow? _selectedServiceLine;
    [ObservableProperty] private int? _bonSortieId;
    [ObservableProperty] private string _bonSortieLabel = string.Empty;
    [ObservableProperty] private bool _canEditStatut = true;

    public bool HasBonSortieLabel => !string.IsNullOrEmpty(BonSortieLabel);
    public bool IsEditable => Statut != StatutReservation.Transformee;

    partial void OnReservationIdChanged(int? value) => RemoveReservationCommand.NotifyCanExecuteChanged();

    partial void OnStatutChanged(StatutReservation value)
    {
        NotifyStatutChip();
        CanEditStatut = false;
        OnPropertyChanged(nameof(IsEditable));
        SyncSelectedStatutOption();
        RemoveReservationCommand.NotifyCanExecuteChanged();
        ToBonSortieCommand.NotifyCanExecuteChanged();
    }

    partial void OnBonSortieLabelChanged(string value) => OnPropertyChanged(nameof(HasBonSortieLabel));

    partial void OnSelectedStatutOptionChanged(SoftReservationStatutOption? value)
    {
        if (value == null || !CanEditStatut) return;
        if (Statut == value.Value) return;
        Statut = value.Value;
    }

    private void NotifyStatutChip()
    {
        StatutLabel = SoftReservationStatutLabels.Format(_locale, Statut);
        StatutChipBackground = SoftReservationStatutLabels.ChipBackground(Statut);
        StatutChipForeground = SoftReservationStatutLabels.ChipForeground(Statut);
        StatutChipBorder = SoftReservationStatutLabels.ChipBorder(Statut);
    }

    private void RefreshStatutOptions()
    {
        var previous = SelectedStatutOption?.Value ?? Statut;
        StatutOptions.Clear();
        foreach (var s in new[] { StatutReservation.Confirmee })
        {
            StatutOptions.Add(new SoftReservationStatutOption
            {
                Value = s,
                Label = SoftReservationStatutLabels.Format(_locale, s)
            });
        }

        if (Statut == StatutReservation.Transformee)
        {
            StatutOptions.Add(new SoftReservationStatutOption
            {
                Value = Statut,
                Label = SoftReservationStatutLabels.Format(_locale, Statut)
            });
        }

        SelectedStatutOption = StatutOptions.FirstOrDefault(o => o.Value == previous)
                               ?? StatutOptions.FirstOrDefault();
    }

    private void SyncSelectedStatutOption()
    {
        if (SelectedStatutOption?.Value == Statut) return;
        SelectedStatutOption = StatutOptions.FirstOrDefault(o => o.Value == Statut)
                               ?? StatutOptions.FirstOrDefault();
    }

    private void ClearBonSortieLinkUi()
    {
        BonSortieId = null;
        BonSortieLabel = string.Empty;
    }

    private async Task RefreshBonSortieLabelAsync(AppDbContext db, int softReservationId, CancellationToken cancellationToken)
    {
        var bs = await db.BonsSortie.AsNoTracking()
            .Where(b => b.ReservationId == softReservationId)
            .Select(b => new { b.Id, b.Numero })
            .FirstOrDefaultAsync(cancellationToken);
        if (bs is null)
        {
            ClearBonSortieLinkUi();
            return;
        }

        BonSortieId = bs.Id;
        BonSortieLabel = string.IsNullOrEmpty(bs.Numero) ? string.Empty : _locale.Tf("SoftRes_BsChip", bs.Numero);
    }

    private bool CanRemoveReservation() => ReservationId != null;

    [RelayCommand(CanExecute = nameof(CanRemoveReservation))]
    private async Task RemoveReservationAsync(CancellationToken cancellationToken)
    {
        if (ReservationId is not { } id) return;

        await using (var dbCheck = await _dbFactory.CreateDbContextAsync(cancellationToken))
        {
            if (await dbCheck.BonsSortie.AsNoTracking().AnyAsync(b => b.ReservationId == id, cancellationToken))
            {
                await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), _locale.T("SoftRes_ErrDeleteHasBs"), cancellationToken);
                return;
            }
        }

        if (!await _dialog.ConfirmAsync(_locale.T("SoftRes_Title"), _locale.Tf("SoftRes_ConfirmDelete", Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.Reservations
                .Include(r => r.ProduitLignes)
                .Include(r => r.ServiceLignes)
                .FirstAsync(r => r.Id == id, cancellationToken);
            db.Reservations.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await _dialog.ShowInfoAsync(_locale.T("SoftRes_Title"), _locale.T("SoftRes_Deleted"), cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression de la réservation soft", ex, "SoftReservationEditViewModel.RemoveReservationAsync");
            await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnAddLineCatalogPickChanged(object? value)
    {
        if (_suppressAddLinePick || !IsEditable) return;
        if (value is not DocumentCatalogItem item) return;
        _suppressAddLinePick = true;
        const decimal addQty = 1;

        if (item.Kind == DocumentCatalogKind.Service)
        {
            var existing = ServiceLignes.FirstOrDefault(l => l.ServiceId == item.Id && item.Id != 0);
            if (existing != null)
            {
                existing.Quantite += addQty;
                SelectedServiceLine = existing;
            }
            else
            {
                var row = new SoftReservationServiceLineRow();
                row.ApplyCatalogItem(item);
                row.Quantite = addQty;
                ServiceLignes.Add(row);
                SelectedServiceLine = row;
            }
        }
        else
        {
            var existing = ProduitLignes.FirstOrDefault(l => l.ProduitId == item.Id && item.Id != 0);
            if (existing != null)
            {
                existing.Quantite += addQty;
                SelectedProduitLine = existing;
            }
            else
            {
                var row = new SoftReservationProduitLineRow();
                row.ApplyCatalogItem(item);
                row.Quantite = addQty;
                ProduitLignes.Add(row);
                SelectedProduitLine = row;
            }
        }

        _addLineSearch.ResetAfterPick(
            () =>
            {
                AddLineCatalogPick = null;
                AddLineSearchText = string.Empty;
            },
            () => _suppressAddLinePick = false);
        RefreshTotals();
    }

    private void ConsolidateDuplicateProductLines()
    {
        foreach (var g in ProduitLignes.Where(l => l.ProduitId is > 0).GroupBy(l => l.ProduitId).ToList())
        {
            if (g.Count() < 2) continue;
            var ordered = g.OrderBy(l => ProduitLignes.IndexOf(l)).ToList();
            var keep = ordered[0];
            var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
            foreach (var line in ordered.Skip(1))
            {
                if (ReferenceEquals(SelectedProduitLine, line))
                    SelectedProduitLine = keep;
                line.PropertyChanged -= ProduitLineOnPropertyChanged;
                ProduitLignes.Remove(line);
            }
            keep.Quantite += extraQty;
        }
    }

    private void ConsolidateDuplicateServiceLines()
    {
        foreach (var g in ServiceLignes.Where(l => l.ServiceId is > 0).GroupBy(l => l.ServiceId).ToList())
        {
            if (g.Count() < 2) continue;
            var ordered = g.OrderBy(l => ServiceLignes.IndexOf(l)).ToList();
            var keep = ordered[0];
            var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
            foreach (var line in ordered.Skip(1))
            {
                if (ReferenceEquals(SelectedServiceLine, line))
                    SelectedServiceLine = keep;
                line.PropertyChanged -= ServiceLineOnPropertyChanged;
                ServiceLignes.Remove(line);
            }
            keep.Quantite += extraQty;
        }
    }

    private void RefreshTotals()
    {
        var ht = ProduitLignes.Sum(l => l.MontantHt) + ServiceLignes.Sum(l => l.MontantHt);
        var tva = ProduitLignes.Sum(l => l.MontantHt * (l.TauxTva / 100m))
                  + ServiceLignes.Sum(l => l.MontantHt * (l.TauxTva / 100m));
        if (RemiseGlobale > 0)
        {
            var factor = 1 - RemiseGlobale / 100m;
            ht *= factor;
            tva *= factor;
        }

        TotalHt = ht;
        TotalTva = tva;
        TotalTtc = ht + tva;
        UpdateTotalLabels(ht, tva, TotalTtc);
    }

    private void UpdateTotalLabels(decimal ht, decimal tva, decimal ttc)
    {
        TotalHtLabel = _locale.Tf("Doc_FmtHt", ht, Devise).TrimEnd();
        TotalTvaLabel = _locale.Tf("Doc_FmtTva", tva, Devise).TrimEnd();
        TotalTtcLabel = _locale.Tf("Doc_FmtTtc", ttc, Devise).TrimEnd();
    }

    partial void OnDeviseChanged(string value) => RefreshTotals();
    partial void OnRemiseGlobaleChanged(decimal value) => RefreshTotals();

    partial void OnSelectedClientChanged(TiersEntity? value)
    {
        var id = value?.Id ?? 0;
        if (ClientId == id) return;
        ClientId = id;
    }

    partial void OnClientIdChanged(int value)
    {
        if (SelectedClient?.Id == value) return;
        SelectedClient = Clients.FirstOrDefault(c => c.Id == value);
    }

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        ReservationId = id;
        ProduitLignes.Clear();
        ServiceLignes.Clear();
        SelectedProduitLine = null;
        SelectedServiceLine = null;
        ResetAddProductSearch();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(cancellationToken);
        Clients.Clear();
        foreach (var c in clients) Clients.Add(c);

        var cfg = await _settings.GetAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);

        if (id == null)
        {
            Numero = "(nouveau)";
            ClientId = Clients.FirstOrDefault()?.Id ?? 0;
            Date = DateTime.Today;
            DateDebut = DateTime.Today;
            DateFinPrevue = DateTime.Today.AddDays(1);
            Statut = StatutReservation.Confirmee;
            Caution = 0;
            RemiseGlobale = 0;
            Note = string.Empty;
            ClearBonSortieLinkUi();
            Title = _locale.T("SoftRes_NewTitle");
            RefreshStatutOptions();
            RefreshTotals();
            return;
        }

        var r = await db.Reservations
            .Include(x => x.ProduitLignes)
            .Include(x => x.ServiceLignes)
            .FirstAsync(x => x.Id == id, cancellationToken);
        Numero = r.Numero;
        ClientId = r.ClientId;
        Date = r.Date.Date;
        DateDebut = r.DateDebut.Date;
        DateFinPrevue = r.DateFinPrevue.Date;
        Statut = SoftReservationStatutLabels.Normalize(r.Statut);
        Caution = r.Caution;
        RemiseGlobale = r.RemiseGlobale;
        Note = r.Note;
        await RefreshBonSortieLabelAsync(db, r.Id, cancellationToken);

        var produitIds = r.ProduitLignes.Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value).Distinct().ToList();
        var serviceIds = r.ServiceLignes.Where(l => l.ServiceId is > 0).Select(l => l.ServiceId!.Value).Distinct().ToList();
        var refs = await db.Produits.AsNoTracking()
            .Where(p => produitIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Reference, cancellationToken);
        var serviceRefs = await db.Services.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Reference, cancellationToken);

        foreach (var l in r.ProduitLignes)
        {
            var reference = l.ProduitId is { } pid && refs.TryGetValue(pid, out var rf) ? rf : string.Empty;
            ProduitLignes.Add(new SoftReservationProduitLineRow
            {
                ProduitId = l.ProduitId,
                Reference = reference,
                Designation = l.Designation,
                Quantite = l.Quantite,
                PrixUnitaireHt = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTva = l.TauxTVA,
                Note = l.Note
            });
        }

        foreach (var l in r.ServiceLignes)
        {
            var reference = l.ServiceId is { } sid && serviceRefs.TryGetValue(sid, out var sr) ? sr : string.Empty;
            ServiceLignes.Add(new SoftReservationServiceLineRow
            {
                ServiceId = l.ServiceId,
                Reference = reference,
                Designation = l.Designation,
                Quantite = l.Quantite,
                PrixUnitaireHt = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTva = l.TauxTVA,
                Note = l.Note
            });
        }

        Title = _locale.Tf("SoftRes_TitleNum", Numero);
        RefreshStatutOptions();
        RefreshTotals();
        ResetAddProductSearch();
        RemoveReservationCommand.NotifyCanExecuteChanged();
        ToBonSortieCommand.NotifyCanExecuteChanged();
    }

    private void ResetAddProductSearch()
    {
        _suppressAddLinePick = true;
        AddLineCatalogPick = null;
        AddLineSearchText = string.Empty;
        _suppressAddLinePick = false;
        _addLineSearch.Clear();
    }

    public void Load(int? id) => _ = LoadAsync(id, CancellationToken.None);

    [RelayCommand]
    private void RemoveProduitLine(SoftReservationProduitLineRow? row)
    {
        if (row == null || !IsEditable) return;
        ProduitLignes.Remove(row);
    }

    [RelayCommand]
    private void RemoveServiceLine(SoftReservationServiceLineRow? row)
    {
        if (row == null || !IsEditable) return;
        ServiceLignes.Remove(row);
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (!IsEditable) return;
        if (SelectedProduitLine != null)
        {
            var line = SelectedProduitLine;
            SelectedProduitLine = null;
            ProduitLignes.Remove(line);
            return;
        }

        if (SelectedServiceLine != null)
        {
            var line = SelectedServiceLine;
            SelectedServiceLine = null;
            ServiceLignes.Remove(line);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!IsEditable)
        {
            await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), _locale.T("SoftRes_ErrReadOnly"), cancellationToken);
            return;
        }

        if (ClientId == 0 || (!ProduitLignes.Any() && !ServiceLignes.Any()))
        {
            await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), _locale.T("SoftRes_ErrClientLines"), cancellationToken);
            return;
        }

        if (Statut != StatutReservation.Confirmee && Statut != StatutReservation.Transformee) Statut = StatutReservation.Confirmee;

        var periodStart = DateDebut.Date;
        var periodEnd = DateFinPrevue.Date;
        if (periodEnd < periodStart)
            (periodStart, periodEnd) = (periodEnd, periodStart);

        var conflicts = await _availability.CheckAsync(
            excludeBonSortieId: null,
            excludeSoftReservationId: ReservationId,
            periodStart,
            periodEnd,
            ProduitLignes
                .Where(l => l.ProduitId is > 0)
                .Select(l => new ReservationAvailabilityLineRequest(
                    l.ProduitId!.Value,
                    l.Quantite,
                    0)),
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var model = BuildAvailabilityWarningModel(periodStart, periodEnd, conflicts);
            if (!await _dialog.ConfirmAvailabilityWarningAsync(model, cancellationToken))
                return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            Models.Reservation entity;
            if (ReservationId == null)
            {
                var num = await _numbers.NextReservationAsync(cancellationToken);
                entity = new Models.Reservation
                {
                    Numero = num,
                    ClientId = ClientId,
                    Date = Date.Date,
                    DateDebut = DateDebut.Date,
                    DateFinPrevue = DateFinPrevue.Date,
                    Statut = Statut,
                    Caution = Caution,
                    RemiseGlobale = RemiseGlobale,
                    Note = Note,
                    CreatedByUserId = _session.UserId
                };
                foreach (var l in ProduitLignes)
                    entity.ProduitLignes.Add(ToProduitEntityLine(l));
                foreach (var l in ServiceLignes)
                    entity.ServiceLignes.Add(ToServiceEntityLine(l));

                db.Reservations.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                ReservationId = entity.Id;
            }
            else
            {
                entity = await db.Reservations
                    .Include(r => r.ProduitLignes)
                    .Include(r => r.ServiceLignes)
                    .FirstAsync(r => r.Id == ReservationId, cancellationToken);
                entity.ClientId = ClientId;
                entity.Date = Date.Date;
                entity.DateDebut = DateDebut.Date;
                entity.DateFinPrevue = DateFinPrevue.Date;
                entity.Statut = Statut;
                entity.Caution = Caution;
                entity.RemiseGlobale = RemiseGlobale;
                entity.Note = Note;
                db.ReservationProduitLignes.RemoveRange(entity.ProduitLignes);
                db.ReservationServiceLignes.RemoveRange(entity.ServiceLignes);
                foreach (var l in ProduitLignes)
                    entity.ProduitLignes.Add(ToProduitEntityLine(l));
                foreach (var l in ServiceLignes)
                    entity.ServiceLignes.Add(ToServiceEntityLine(l));

                await db.SaveChangesAsync(cancellationToken);
            }

            // Soft booking: no ResyncStockAsync
            Numero = entity.Numero;
            await _dialog.ShowInfoAsync(_locale.T("SoftRes_Title"), _locale.T("SoftRes_Saved"), cancellationToken);
            await LoadAsync(ReservationId, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'enregistrement de la réservation soft", ex, "SoftReservationEditViewModel.SaveAsync");
            await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private AvailabilityWarningDialogModel BuildAvailabilityWarningModel(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyList<ReservationAvailabilityConflict> conflicts)
    {
        var products = conflicts.Select(c =>
        {
            var productTitle = string.IsNullOrWhiteSpace(c.Reference)
                ? c.Designation
                : $"{c.Designation}  ({c.Reference})";

            return new AvailabilityWarningProductBlock
            {
                ProductTitle = productTitle,
                DemandeLabel = _locale.T("Loc_AvailWarnDemande"),
                DemandeValue = c.Demande.ToString("N0"),
                DisponibleLabel = _locale.T("Loc_AvailWarnDisponible"),
                DisponibleValue = c.Disponible.ToString("N0"),
                StockLabel = _locale.T("Loc_AvailWarnStock"),
                StockValue = c.StockTotal.ToString("N0"),
                DispoStockLabel = _locale.T("Loc_AvailWarnDispoStock"),
                DispoStockValue = c.DispoStock.ToString("N0"),
                ReserveLabel = _locale.T("Loc_AvailWarnReserve"),
                ReserveValue = c.DejaReserve.ToString("N0"),
                SortieLabel = _locale.T("Loc_AvailWarnSortie"),
                SortieValue = c.DejaSortie.ToString("N0"),
                ConflictsHeader = c.Sources.Count > 0 ? _locale.T("Loc_AvailWarnConflicts") : null,
                Conflicts = c.Sources.Take(8).Select(s => new AvailabilityWarningConflictChip
                {
                    Title = $"{s.Numero} — {s.ClientNom}",
                    Detail = $"{s.DateDebut:dd/MM/yyyy} → {s.DateFin:dd/MM/yyyy}  ·  {s.QuantiteEncore:N0}"
                }).ToList()
            };
        }).ToList();

        return new AvailabilityWarningDialogModel
        {
            Title = _locale.T("Loc_AvailTitle"),
            Header = _locale.T("Loc_AvailWarnHeader"),
            PeriodText = _locale.Tf("Loc_AvailWarnPeriod", periodStart, periodEnd),
            ConfirmQuestion = _locale.T("Loc_AvailWarnConfirm"),
            YesLabel = _locale.T("Btn_Yes"),
            NoLabel = _locale.T("Btn_No"),
            Products = products
        };
    }

    private ReservationProduitLigne ToProduitEntityLine(SoftReservationProduitLineRow l) => new()
    {
        ProduitId = l.ProduitId,
        Designation = l.Designation,
        Quantite = l.Quantite,
        PrixUnitaireHT = l.PrixUnitaireHt,
        Remise = l.Remise,
        TauxTVA = l.TauxTva,
        Note = l.Note,
        CreatedByUserId = _session.UserId
    };

    private static ReservationServiceLigne ToServiceEntityLine(SoftReservationServiceLineRow l) => new()
    {
        ServiceId = l.ServiceId,
        Designation = l.Designation,
        Quantite = l.Quantite,
        PrixUnitaireHT = l.PrixUnitaireHt,
        Remise = l.Remise,
        TauxTVA = l.TauxTva,
        Note = l.Note
    };

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<SoftReservationListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }

    private bool CanToBonSortie() =>
        ReservationId != null;

    [RelayCommand(CanExecute = nameof(CanToBonSortie))]
    private async Task ToBonSortieAsync(CancellationToken cancellationToken)
    {
        if (ReservationId is not { } resId)
        {
            await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), _locale.T("SoftRes_ToBsNeedSave"), cancellationToken);
            return;
        }

        if (BonSortieId is { } existingBsId && Statut == StatutReservation.Transformee)
        {
            OpenBonSortie(existingBsId);
            return;
        }

        if (ClientId == 0 || (!ProduitLignes.Any() && !ServiceLignes.Any()))
        {
            await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), _locale.T("SoftRes_ErrClientLines"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            var bsId = await _transform.TransformToBonSortieAsync(resId, _session.UserId, cancellationToken);
            BonSortieId = bsId;
            Statut = StatutReservation.Transformee;
            OpenBonSortie(bsId);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec Vers bon de sortie depuis réservation soft", ex, "SoftReservationEditViewModel.ToBonSortieAsync");
            await _dialog.ShowErrorAsync(_locale.T("SoftRes_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenBonSortie(int bsId)
    {
        var vm = _sp.GetRequiredService<ReservationEditViewModel>();
        vm.Load(bsId);
        _workspace.Open(vm);
    }
}
