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
    ReturnDueSoon,
    SoftReservationStartingSoon,
    AvailabilityConflict,
    LongOverdueMaterial,
    StockBelowMin,
    HighDemandSoon,
    ReturnConditionAction,
    UnpaidInvoice
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
        string detail,
        DashboardAlertNav nav,
        int? entityId,
        string? productLabel = null)
    {
        Kind = kind;
        Severity = severity;
        Category = category;
        Title = title;
        Detail = detail;
        Nav = nav;
        EntityId = entityId;
        ProductLabel = productLabel;
    }

    public DashboardAlertKind Kind { get; }
    public DashboardAlertSeverity Severity { get; }
    public string Category { get; }
    public string Title { get; }
    public string Detail { get; }
    public DashboardAlertNav Nav { get; }
    public int? EntityId { get; }
    public string? ProductLabel { get; }

    public bool IsCritical => Severity == DashboardAlertSeverity.Critical;
    public bool IsWarning => Severity == DashboardAlertSeverity.Warning;
    public bool IsInfo => Severity == DashboardAlertSeverity.Info;
}
