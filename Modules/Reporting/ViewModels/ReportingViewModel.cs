using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.Reporting.Services;
using GestionCommerciale.Modules.Reservation.ViewModels;
using GestionCommerciale.Modules.Stock.ViewModels;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Reporting.ViewModels;

public partial class ReportingViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly IAppSettingsService _settings;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IDashboardAlertsService _dashAlerts;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;

    private ReportData? _cachedData;

    public ReportingViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        IAppSettingsService settings,
        ICurrentUserSession session,
        ILocaleService locale,
        IDashboardAlertsService dashAlerts,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _settings = settings;
        _session = session;
        _locale = locale;
        _dashAlerts = dashAlerts;
        _workspace = workspaceNavigator;
        _sp = sp;
        _locale.CultureApplied += (_, _) => RefreshReportingUi();
        RefreshReportingUi();
        Title = _locale.T("Report_Title");
    }

    [ObservableProperty] private string _lblCa = string.Empty;
    [ObservableProperty] private string _lblCaDelta = string.Empty;
    [ObservableProperty] private string _lblKpiStrip = string.Empty;
    [ObservableProperty] private string _lblTopClients = string.Empty;
    [ObservableProperty] private string _lblTopProducts = string.Empty;
    [ObservableProperty] private string _lblDashAlerts = string.Empty;
    [ObservableProperty] private string _dashAlertHint = string.Empty;
    [ObservableProperty] private string _lineCaCurrent = string.Empty;
    [ObservableProperty] private string _lineCaPrev = string.Empty;
    [ObservableProperty] private string _lineCaDelta = string.Empty;
    [ObservableProperty] private string _lblLoading = string.Empty;

    [ObservableProperty] private string _caMoisCourant = string.Empty;
    [ObservableProperty] private string _caMoisPrecedent = string.Empty;

    [ObservableProperty] private string _kpiDevis30 = string.Empty;
    [ObservableProperty] private string _kpiDevisExpire = string.Empty;
    [ObservableProperty] private string _kpiBlMonth = string.Empty;
    [ObservableProperty] private string _kpiBc = string.Empty;
    [ObservableProperty] private string _kpiBrMonth = string.Empty;
    [ObservableProperty] private string _kpiEncours = string.Empty;
    [ObservableProperty] private string _kpiStock = string.Empty;

    [ObservableProperty] private bool _showEmptyTopClients;
    [ObservableProperty] private bool _showEmptyTopProducts;
    [ObservableProperty] private bool _showEmptyDashAlerts;

    [ObservableProperty] private string _emptyMessageTopClients = string.Empty;
    [ObservableProperty] private string _emptyMessageTopProducts = string.Empty;
    [ObservableProperty] private string _emptyMessageDashAlerts = string.Empty;

    public ObservableCollection<ReportRankRow> TopClients { get; } = [];
    public ObservableCollection<ReportRankRow> TopProduits { get; } = [];
    public ObservableCollection<DashboardAlertRow> DashAlerts { get; } = [];

    private void RefreshReportingUi()
    {
        Title = _locale.T("Report_Title");
        LblLoading = _locale.T("Report_Loading");
        LblCa = _locale.T("Report_LblCa");
        LblCaDelta = _locale.T("Report_LblCaDelta");
        LblKpiStrip = _locale.T("Report_LblKpiStrip");
        LblTopClients = _locale.T("Report_LblTopClients");
        LblTopProducts = _locale.T("Report_LblTopProducts");
        LblDashAlerts = _locale.T("Report_LblDashAlerts");
        DashAlertHint = _locale.T("Report_DashAlertHint");
        LineCaCurrent = _locale.Tf("Report_FmtCurrentMonth", CaMoisCourant);
        LineCaPrev = _locale.Tf("Report_FmtPrevMonth", CaMoisPrecedent);
        EmptyMessageTopClients = _locale.T("Report_EmptyTopClients");
        EmptyMessageTopProducts = _locale.T("Report_EmptyTopProducts");
        EmptyMessageDashAlerts = _locale.T("Report_EmptyDashAlerts");
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessReporting)
        {
            await _dialog.ShowErrorAsync(_locale.T("Report_Title"), _locale.T("Report_ErrDenied"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Yield();

            var data = await Task.Run(() => LoadDataAsync(cancellationToken), cancellationToken);
            var dash = await Task.Run(() => _dashAlerts.GetAlertsAsync(cancellationToken), cancellationToken);

            _cachedData = data;
            ApplyData(data);
            ApplyDashAlerts(dash);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            AppLog.Error("Échec du chargement des données de reporting", ex, "ReportingViewModel.LoadAsync");
            await _dialog.ShowErrorAsync(_locale.T("Report_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyDashAlerts(IReadOnlyList<DashboardAlertRow> rows)
    {
        DashAlerts.Clear();
        foreach (var r in rows)
            DashAlerts.Add(r);
        ShowEmptyDashAlerts = DashAlerts.Count == 0;
    }

    [RelayCommand]
    private void OpenDashAlert(DashboardAlertRow? row)
    {
        if (row is null) return;

        switch (row.Nav)
        {
            case DashboardAlertNav.BonSortie when row.EntityId is { } bsId:
            {
                var vm = _sp.GetRequiredService<ReservationEditViewModel>();
                vm.Load(bsId);
                _workspace.Open(vm);
                break;
            }
            case DashboardAlertNav.SoftReservation when row.EntityId is { } softId:
            {
                var vm = _sp.GetRequiredService<SoftReservationEditViewModel>();
                vm.Load(softId);
                _workspace.Open(vm);
                break;
            }
            case DashboardAlertNav.Facture when row.EntityId is { } factureId:
            {
                var vm = _sp.GetRequiredService<FactureEditViewModel>();
                vm.Load(factureId);
                _workspace.Open(vm);
                break;
            }
            case DashboardAlertNav.Availability when row.EntityId is { } produitId:
            {
                var vm = _sp.GetRequiredService<ProductAvailabilityViewModel>();
                vm.ShowProduct(produitId, row.ProductLabel);
                _workspace.Open(vm);
                break;
            }
            case DashboardAlertNav.Produits:
            {
                _workspace.Open(_sp.GetRequiredService<ProduitsViewModel>());
                break;
            }
        }
    }

    private void ApplyData(ReportData data)
    {
        CaMoisCourant = data.CaMoisCourant;
        CaMoisPrecedent = data.CaMoisPrecedent;
        LineCaCurrent = data.LineCaCurrent;
        LineCaPrev = data.LineCaPrev;
        LineCaDelta = data.LineCaDelta;
        KpiDevis30 = data.KpiDevis30;
        KpiDevisExpire = data.KpiDevisExpire;
        KpiBlMonth = data.KpiBlMonth;
        KpiBc = data.KpiBc;
        KpiBrMonth = data.KpiBrMonth;
        KpiStock = data.KpiStock;
        KpiEncours = data.KpiEncours;

        TopClients.Clear();
        foreach (var r in data.TopClients)
            TopClients.Add(r);
        ShowEmptyTopClients = TopClients.Count == 0;

        TopProduits.Clear();
        foreach (var r in data.TopProduits)
            TopProduits.Add(r);
        ShowEmptyTopProducts = TopProduits.Count == 0;
    }

    private async Task<ReportData> LoadDataAsync(CancellationToken ct)
    {
        var cfg = await _settings.GetAsync(ct);
        var dev = string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise!;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.Today;
        var startCur = new DateTime(now.Year, now.Month, 1);
        var startPrev = startCur.AddMonths(-1);
        var endCur = startCur.AddMonths(1);
        var endPrev = startCur;
        var since30 = now.AddDays(-30);
        var expireUntil = now.AddDays(14);

        var caCur = await InvoiceTtcSumAsync(db, startCur, endCur, ct);
        var caPrev = await InvoiceTtcSumAsync(db, startPrev, endPrev, ct);

        var devis30 = await db.Devis.AsNoTracking().CountAsync(d => d.Date >= since30, ct);
        var devisExpire = await db.Devis.AsNoTracking().CountAsync(
            d => d.DateValidite >= now && d.DateValidite <= expireUntil, ct);
        var blMonth = await db.BonsLivraison.AsNoTracking().CountAsync(
            b => b.Date >= startCur && b.Date < endCur, ct);
        var bcMonth = await db.BonsCommande.AsNoTracking().CountAsync(
            b => b.Date >= startCur && b.Date < endCur, ct);
        var bcTotal = await db.BonsCommande.AsNoTracking().CountAsync(ct);
        var brMonth = await db.BonsReception.AsNoTracking().CountAsync(
            b => b.Date >= startCur && b.Date < endCur, ct);

        var yearStart = startCur.AddMonths(-11);
        var topClientAgg = (await db.Factures.AsNoTracking()
            .Where(f => f.Date >= yearStart)
            .Select(f => new {
                f.ClientId,
                TTC = f.Lignes.Sum(l => l.Quantite * l.PrixUnitaireHT * (1m - l.Remise / 100m) * (1m + l.TauxTVA / 100m)) * (1m - f.RemiseGlobale / 100m)
            })
            .GroupBy(x => x.ClientId)
            .Select(g => new { ClientId = g.Key, Total = g.Sum(x => x.TTC) })
            .ToListAsync(ct))
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToList();

        var maxClient = topClientAgg.Count > 0 ? topClientAgg.Max(x => x.Total) : 0m;

        var topClientRows = new List<ReportRankRow>();
        foreach (var x in topClientAgg)
        {
            var nom = await db.Tiers.AsNoTracking().Where(t => t.Id == x.ClientId).Select(t => t.Nom).FirstOrDefaultAsync(ct);
            var share = maxClient > 0 ? (double)(x.Total / maxClient) : 0;
            topClientRows.Add(new ReportRankRow(
                nom ?? string.Empty,
                CurrencyHelper.Format(x.Total, dev),
                share));
        }

        var blSince = startCur.AddMonths(-11);
        var blLignes = await (
            from l in db.BonLivraisonLignes.AsNoTracking()
            join b in db.BonsLivraison.AsNoTracking() on l.BLId equals b.Id
            where b.Date >= blSince
            select new { l.ProduitId, l.QuantiteLivree }
        ).ToListAsync(ct);
        var topProd = blLignes
            .GroupBy(l => l.ProduitId)
            .Select(g => new { ProduitId = g.Key, Qty = g.Sum(x => x.QuantiteLivree) })
            .OrderByDescending(x => x.Qty)
            .Take(5)
            .ToList();

        var maxQty = topProd.Count > 0 ? topProd.Max(x => x.Qty) : 0m;

        var topProdRows = new List<ReportRankRow>();
        foreach (var x in topProd)
        {
            var nom = await db.Produits.AsNoTracking().Where(p => p.Id == x.ProduitId).Select(p => p.Designation).FirstOrDefaultAsync(ct);
            var share = maxQty > 0 ? (double)(x.Qty / maxQty) : 0;
            topProdRows.Add(new ReportRankRow(
                nom ?? string.Empty,
                x.Qty.ToString("N2", CultureInfo.CurrentCulture),
                share));
        }

        var actifs = await db.Produits.AsNoTracking().CountAsync(p => p.Actif, ct);
        var sousMin = await db.Produits.AsNoTracking().CountAsync(
            p => p.Actif && p.StockMinimum > 0 && p.StockActuel < p.StockMinimum, ct);
        var pctSous = actifs > 0 ? (double)sousMin / actifs * 100.0 : 0;

        var unpaidProj = await db.Factures.AsNoTracking()
            .Where(f => !f.EstPayee)
            .Select(f => new {
                TTC = f.Lignes.Sum(l => l.Quantite * l.PrixUnitaireHT * (1m - l.Remise / 100m) * (1m + l.TauxTVA / 100m)) * (1m - f.RemiseGlobale / 100m),
                Paye = f.Paiements.Sum(p => (decimal?)p.Montant) ?? 0m
            })
            .ToListAsync(ct);

        decimal encoursTotal = 0;
        var encoursCount = 0;
        foreach (var f in unpaidProj)
        {
            var reste = f.TTC - f.Paye;
            if (reste <= 0.01m) continue;
            encoursTotal += reste;
            encoursCount++;
        }

        return new ReportData
        {
            Devise = dev,
            CaMoisCourant = CurrencyHelper.Format(caCur, dev),
            CaMoisPrecedent = CurrencyHelper.Format(caPrev, dev),
            LineCaCurrent = FormatCaLine(_locale, "Report_FmtCurrentMonth", caCur, dev),
            LineCaPrev = FormatCaLine(_locale, "Report_FmtPrevMonth", caPrev, dev),
            LineCaDelta = FormatCaDelta(caCur, caPrev, dev, _locale),
            KpiDevis30 = _locale.Tf("Report_KpiDevis30", devis30.ToString(CultureInfo.CurrentCulture)),
            KpiDevisExpire = _locale.Tf("Report_KpiDevisExpire", devisExpire.ToString(CultureInfo.CurrentCulture)),
            KpiBlMonth = _locale.Tf("Report_KpiBlMonth", blMonth.ToString(CultureInfo.CurrentCulture)),
            KpiBc = _locale.Tf("Report_KpiBc", bcMonth.ToString(CultureInfo.CurrentCulture), bcTotal.ToString(CultureInfo.CurrentCulture)),
            KpiBrMonth = _locale.Tf("Report_KpiBrMonth", brMonth.ToString(CultureInfo.CurrentCulture)),
            KpiStock = _locale.Tf("Report_KpiStock", actifs.ToString(CultureInfo.CurrentCulture), sousMin.ToString(CultureInfo.CurrentCulture), pctSous.ToString("F0", CultureInfo.CurrentCulture)),
            KpiEncours = _locale.Tf("Report_KpiEncours", CurrencyHelper.Format(encoursTotal, dev), encoursCount.ToString(CultureInfo.CurrentCulture)),
            TopClients = topClientRows,
            TopProduits = topProdRows,
        };
    }

    private static async Task<decimal> InvoiceTtcSumAsync(AppDbContext db, DateTime from, DateTime to, CancellationToken ct)
    {
        return await db.Factures.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < to)
            .Select(f => (decimal?)f.Lignes.Sum(l => l.Quantite * l.PrixUnitaireHT * (1m - l.Remise / 100m) * (1m + l.TauxTVA / 100m)) * (1m - f.RemiseGlobale / 100m))
            .SumAsync(ct) ?? 0m;
    }

    private static string FormatCaLine(ILocaleService locale, string key, decimal amount, string dev)
        => locale.Tf(key, CurrencyHelper.Format(amount, dev));

    private static string FormatCaDelta(decimal caCur, decimal caPrev, string dev, ILocaleService locale)
    {
        if (Math.Abs(caPrev) < 0.01m && Math.Abs(caCur) < 0.01m)
            return locale.T("Report_FmtCaDeltaZero");
        if (Math.Abs(caPrev) < 0.01m)
            return locale.T("Report_FmtCaDeltaFromZero");

        var diff = caCur - caPrev;
        var pct = (double)(diff / caPrev * 100m);
        return locale.Tf("Report_FmtCaDeltaFmt",
            CurrencyHelper.Format(diff, dev),
            pct.ToString("F1", CultureInfo.CurrentCulture));
    }
}

internal sealed class ReportData
{
    public string Devise { get; init; } = string.Empty;
    public string CaMoisCourant { get; init; } = string.Empty;
    public string CaMoisPrecedent { get; init; } = string.Empty;
    public string LineCaCurrent { get; init; } = string.Empty;
    public string LineCaPrev { get; init; } = string.Empty;
    public string LineCaDelta { get; init; } = string.Empty;
    public string KpiDevis30 { get; init; } = string.Empty;
    public string KpiDevisExpire { get; init; } = string.Empty;
    public string KpiBlMonth { get; init; } = string.Empty;
    public string KpiBc { get; init; } = string.Empty;
    public string KpiBrMonth { get; init; } = string.Empty;
    public string KpiStock { get; init; } = string.Empty;
    public string KpiEncours { get; init; } = string.Empty;
    public List<ReportRankRow> TopClients { get; init; } = [];
    public List<ReportRankRow> TopProduits { get; init; } = [];
}
