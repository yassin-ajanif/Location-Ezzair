namespace GestionCommerciale.Modules.Reporting.ViewModels;

public enum DashboardAlertSeverity
{
    Critical,
    Warning,
    Info
}

public enum DashboardAlertKind
{
    ReturnOverdue,
    SoftReservationExpired
}

public enum DashboardAlertNav
{
    BonSortie,
    SoftReservation,
    Availability,
    Produits,
    Facture
}

public sealed class DashboardAlertRow
{
    public DashboardAlertRow(
        DashboardAlertKind kind,
        DashboardAlertSeverity severity,
        string category,
        string title,
        string detailBefore,
        string daysText,
        string detailAfter,
        DashboardAlertNav nav,
        int? entityId,
        string? productLabel = null)
    {
        Kind = kind;
        Severity = severity;
        Category = category;
        Title = title;
        DetailBefore = detailBefore;
        DaysText = daysText;
        DetailAfter = detailAfter;
        Nav = nav;
        EntityId = entityId;
        ProductLabel = productLabel;
    }

    public DashboardAlertKind Kind { get; }
    public DashboardAlertSeverity Severity { get; }
    public string Category { get; }
    public string Title { get; }
    public string DetailBefore { get; }
    public string DaysText { get; }
    public string DetailAfter { get; }
    public string Detail => $"{DetailBefore}{DaysText}{DetailAfter}";
    public DashboardAlertNav Nav { get; }
    public int? EntityId { get; }
    public string? ProductLabel { get; }

    public bool IsCritical => Severity == DashboardAlertSeverity.Critical;
    public bool IsWarning => Severity == DashboardAlertSeverity.Warning;
    public bool IsInfo => Severity == DashboardAlertSeverity.Info;
}
