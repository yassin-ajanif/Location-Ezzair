using GestionCommerciale.Modules.Reporting.ViewModels;

namespace GestionCommerciale.Modules.Reporting.Services;

public interface IDashboardAlertsService
{
    Task<IReadOnlyList<DashboardAlertRow>> GetAlertsAsync(CancellationToken cancellationToken = default);
}
