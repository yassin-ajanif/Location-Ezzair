using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Reservation.Services;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class ProductAvailabilityViewModel : BaseViewModel
{
    private readonly IReservationAvailabilityService _availability;
    private readonly ICatalogSearchService _catalog;
    private readonly ILocaleService _locale;
    private CancellationTokenSource? _searchCts;
    private bool _suppressPick;

    public ProductAvailabilityViewModel(
        IReservationAvailabilityService availability,
        ICatalogSearchService catalog,
        ILocaleService locale)
    {
        _availability = availability;
        _catalog = catalog;
        _locale = locale;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
        _ = LoadMonthAsync(CancellationToken.None);
    }

    [ObservableProperty] private string _lblProduct = string.Empty;
    [ObservableProperty] private string _wmProduct = string.Empty;
    [ObservableProperty] private string _lblQtyNeeded = string.Empty;
    [ObservableProperty] private string _lblStockTotal = string.Empty;
    [ObservableProperty] private string _lblLegendFree = string.Empty;
    [ObservableProperty] private string _lblLegendPartial = string.Empty;
    [ObservableProperty] private string _lblLegendFull = string.Empty;
    [ObservableProperty] private string _lblFreeWindows = string.Empty;
    [ObservableProperty] private string _lblBookings = string.Empty;
    [ObservableProperty] private string _lblEmptyProduct = string.Empty;
    [ObservableProperty] private string _lblNoFree = string.Empty;
    [ObservableProperty] private string _lblNoBookings = string.Empty;
    [ObservableProperty] private string _lblToday = string.Empty;
    [ObservableProperty] private string _monthTitle = string.Empty;
    [ObservableProperty] private string _productSummary = string.Empty;
    [ObservableProperty] private string _stockTotalLabel = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private object? _productPick;
    [ObservableProperty] private int? _selectedProduitId;
    [ObservableProperty] private decimal _qtyNeeded = 1;
    [ObservableProperty] private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public ObservableCollection<DocumentCatalogItem> SearchResults { get; } = [];
    public ObservableCollection<AvailabilityDayCell> Days { get; } = [];
    public ObservableCollection<AvailabilityWeekRow> Weeks { get; } = [];
    public ObservableCollection<string> WeekdayHeaders { get; } = [];
    public ObservableCollection<AvailabilityFreeWindowRow> FreeWindows { get; } = [];
    public ObservableCollection<AvailabilityBookingRow> Bookings { get; } = [];

    public AutoCompleteFilterPredicate<object?> CatalogFilter => DocumentCatalogAutoComplete.ItemFilter;
    public bool HasProduct => SelectedProduitId is > 0;
    public bool HasFreeWindows => FreeWindows.Count > 0;
    public bool HasBookings => Bookings.Count > 0;

    private void RefreshUi()
    {
        Title = _locale.T("Nav_Availability");
        LblProduct = _locale.T("Avail_LblProduct");
        WmProduct = _locale.T("Avail_WmProduct");
        LblQtyNeeded = _locale.T("Avail_LblQtyNeeded");
        LblStockTotal = _locale.T("Avail_LblStockTotal");
        LblLegendFree = _locale.T("Avail_LegendFree");
        LblLegendPartial = _locale.T("Avail_LegendPartial");
        LblLegendFull = _locale.T("Avail_LegendFull");
        LblFreeWindows = _locale.T("Avail_FreeWindows");
        LblBookings = _locale.T("Avail_Bookings");
        LblEmptyProduct = _locale.T("Avail_EmptyProduct");
        LblNoFree = _locale.T("Avail_NoFree");
        LblNoBookings = _locale.T("Avail_NoBookings");
        LblToday = _locale.T("Avail_Today");
        WeekdayHeaders.Clear();
        foreach (var key in new[]
                 {
                     "Avail_DowMon", "Avail_DowTue", "Avail_DowWed", "Avail_DowThu",
                     "Avail_DowFri", "Avail_DowSat", "Avail_DowSun"
                 })
            WeekdayHeaders.Add(_locale.T(key));
        RefreshMonthTitle();
    }

    private void RefreshMonthTitle() =>
        MonthTitle = Month.ToString("MMMM yyyy", CultureInfo.CurrentUICulture);

    partial void OnSearchTextChanged(string value)
    {
        if (_suppressPick) return;
        _ = SearchProductsAsync(value);
    }

    partial void OnProductPickChanged(object? value)
    {
        if (_suppressPick) return;
        if (value is not DocumentCatalogItem { Kind: DocumentCatalogKind.Product } item)
            return;

        SelectedProduitId = item.Id;
        ProductSummary = $"{item.Designation}  ({item.Reference})";
        OnPropertyChanged(nameof(HasProduct));
        _suppressPick = true;
        SearchText = $"{item.Reference} — {item.Designation}";
        _suppressPick = false;
        _ = LoadMonthAsync(CancellationToken.None);
    }

    /// <summary>Open calendar focused on a product (e.g. from Accueil alerts).</summary>
    public void ShowProduct(int produitId, string? label = null)
    {
        if (produitId <= 0) return;
        SelectedProduitId = produitId;
        var text = string.IsNullOrWhiteSpace(label) ? $"#{produitId}" : label.Trim();
        ProductSummary = text;
        OnPropertyChanged(nameof(HasProduct));
        _suppressPick = true;
        SearchText = text;
        _suppressPick = false;
        _ = LoadMonthAsync(CancellationToken.None);
    }

    partial void OnQtyNeededChanged(decimal value) => _ = LoadMonthAsync(CancellationToken.None);

    [RelayCommand]
    private void PrevMonth()
    {
        Month = Month.AddMonths(-1);
        RefreshMonthTitle();
        _ = LoadMonthAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void NextMonth()
    {
        Month = Month.AddMonths(1);
        RefreshMonthTitle();
        _ = LoadMonthAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void GoTodayMonth()
    {
        Month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RefreshMonthTitle();
        _ = LoadMonthAsync(CancellationToken.None);
    }

    private async Task SearchProductsAsync(string? text)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;
        try
        {
            await Task.Delay(120, ct);
            var products = await _catalog.SearchProductsAsync(text ?? string.Empty, 25, ct);
            SearchResults.Clear();
            foreach (var p in products)
                SearchResults.Add(DocumentCatalogItem.FromProduct(p));
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    private async Task LoadMonthAsync(CancellationToken cancellationToken)
    {
        Days.Clear();
        Weeks.Clear();
        FreeWindows.Clear();
        Bookings.Clear();
        StockTotalLabel = string.Empty;
        OnPropertyChanged(nameof(HasFreeWindows));
        OnPropertyChanged(nameof(HasBookings));

        if (SelectedProduitId is not { } pid)
            return;

        var result = await _availability.GetProductMonthAsync(pid, Month, QtyNeeded, cancellationToken);
        if (result is null)
            return;

        ProductSummary = $"{result.Designation}  ({result.Reference})";
        StockTotalLabel = _locale.Tf("Avail_StockTotalFmt", result.StockTotal);

        // Prefer the first free-window start (matches "Prochaines disponibilités").
        DateTime? nextAvailable = result.FreeWindows.Count > 0
            ? result.FreeWindows[0].DateDebut.Date
            : result.Days
                .Where(d => d.IsCurrentMonth && d.Date.Date >= DateTime.Today && d.Available >= QtyNeeded)
                .Select(d => (DateTime?)d.Date.Date)
                .FirstOrDefault();

        foreach (var d in result.Days)
            Days.Add(AvailabilityDayCell.From(d, QtyNeeded, nextAvailable));

        for (var w = 0; w < 6; w++)
        {
            var week = new AvailabilityWeekRow();
            for (var i = 0; i < 7; i++)
            {
                var idx = w * 7 + i;
                if (idx < Days.Count)
                    week.Days.Add(Days[idx]);
            }
            Weeks.Add(week);
        }

        foreach (var w in result.FreeWindows)
            FreeWindows.Add(new AvailabilityFreeWindowRow(
                _locale.Tf("Avail_FreeWindowFmt", w.DateDebut, w.DateFin, w.AvailableMin)));

        foreach (var b in result.UpcomingBookings)
            Bookings.Add(new AvailabilityBookingRow(
                $"{b.Numero} — {b.ClientNom}",
                _locale.Tf("Avail_BookingDetailFmt", b.DateDebut, b.DateFin, b.QuantiteEncore)));

        OnPropertyChanged(nameof(HasFreeWindows));
        OnPropertyChanged(nameof(HasBookings));
    }
}

public sealed class AvailabilityWeekRow
{
    public ObservableCollection<AvailabilityDayCell> Days { get; } = [];
}

public partial class AvailabilityDayCell : ObservableObject
{
    private static readonly IBrush OutsideBg = Brush.Parse("#F3F0EA");
    private static readonly IBrush FreeBg = Brush.Parse("#DCFCE7");
    private static readonly IBrush FreeBorder = Brush.Parse("#86EFAC");
    private static readonly IBrush FreeFg = Brush.Parse("#166534");
    private static readonly IBrush PartialBg = Brush.Parse("#FEF3C7");
    private static readonly IBrush PartialBorder = Brush.Parse("#FCD34D");
    private static readonly IBrush PartialFg = Brush.Parse("#92400E");
    private static readonly IBrush FullBg = Brush.Parse("#FEE2E2");
    private static readonly IBrush FullBorder = Brush.Parse("#FECACA");
    private static readonly IBrush FullFg = Brush.Parse("#991B1B");
    private static readonly IBrush TodayBorder = Brush.Parse("#C7D2FE");
    private static readonly IBrush NextBorder = Brush.Parse("#3730A3");

    public DateTime Date { get; }
    public string DayNumber { get; }
    public string RestantLine { get; }
    public string DispoStockLine { get; }
    public string SortieLine { get; }
    public string ReserveLine { get; }
    public string RetardLine { get; }
    public bool ShowRetard { get; }
    public bool ShowSortie { get; }
    public IBrush Background { get; }
    public IBrush BorderBrush { get; }
    public IBrush Foreground { get; }
    public bool IsCurrentMonth { get; }
    public bool IsNextAvailable { get; }
    public double Opacity { get; }
    public Thickness CellBorderThickness { get; }

    [ObservableProperty] private bool _isExpanded;

    public string ExpandArrow => IsExpanded ? "▴" : "▾";

    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(ExpandArrow));

    [RelayCommand]
    private void ToggleDetails() => IsExpanded = !IsExpanded;

    private AvailabilityDayCell(
        DateTime date,
        string dayNumber,
        string restantLine,
        string dispoStockLine,
        string sortieLine,
        string reserveLine,
        string retardLine,
        bool showRetard,
        bool showSortie,
        IBrush background,
        IBrush borderBrush,
        IBrush foreground,
        bool isCurrentMonth,
        bool isNextAvailable,
        double opacity)
    {
        Date = date;
        DayNumber = dayNumber;
        RestantLine = restantLine;
        DispoStockLine = dispoStockLine;
        SortieLine = sortieLine;
        ReserveLine = reserveLine;
        RetardLine = retardLine;
        ShowRetard = showRetard;
        ShowSortie = showSortie;
        Background = background;
        BorderBrush = borderBrush;
        Foreground = foreground;
        IsCurrentMonth = isCurrentMonth;
        IsNextAvailable = isNextAvailable;
        Opacity = opacity;
        CellBorderThickness = isNextAvailable ? new Thickness(3) : new Thickness(1);
    }

    public static AvailabilityDayCell From(
        ProductAvailabilityDay day,
        decimal qtyNeeded,
        DateTime? nextAvailableDate)
    {
        var isToday = day.Date.Date == DateTime.Today;
        var isNext = nextAvailableDate is { } n && day.Date.Date == n.Date;

        IBrush bg, border, fg;
        switch (day.Level)
        {
            case ProductAvailabilityDayLevel.Free:
                bg = FreeBg; border = FreeBorder; fg = FreeFg;
                break;
            case ProductAvailabilityDayLevel.Partial:
                bg = PartialBg; border = PartialBorder; fg = PartialFg;
                break;
            case ProductAvailabilityDayLevel.Full:
                bg = FullBg; border = FullBorder; fg = FullFg;
                break;
            default:
                bg = OutsideBg; border = Brush.Parse("#E8DFD0"); fg = Brush.Parse("#9CA3AF");
                break;
        }

        if (isNext)
            border = NextBorder;
        else if (isToday && day.IsCurrentMonth)
            border = TodayBorder;

        var showRetard = day.RetardQty > 0;
        return new AvailabilityDayCell(
            day.Date,
            day.Date.Day.ToString(),
            $"{day.Available:N0} dispo",
            $"{day.DispoStock:N0} dispo stock",
            $"{day.BonSortieBooked:N0} sortie",
            $"{day.SoftBooked:N0} réservé",
            $"{day.RetardQty:N0} retard",
            showRetard,
            showSortie: !showRetard,
            bg,
            border,
            fg,
            day.IsCurrentMonth,
            isNext,
            day.IsCurrentMonth ? 1 : 0.45);
    }
}

public sealed record AvailabilityFreeWindowRow(string Label);
public sealed record AvailabilityBookingRow(string Title, string Detail);

