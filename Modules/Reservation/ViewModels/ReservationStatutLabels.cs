using Avalonia.Media;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public static class ReservationStatutLabels
{
    public static string Format(ILocaleService locale, StatutBonSortie s) =>
        locale.T(s switch
        {
            StatutBonSortie.EnCours => "Loc_Statut_EnCours",
            StatutBonSortie.PartiellementRetournee => "Loc_Statut_Partiel",
            StatutBonSortie.Retournee => "Loc_Statut_Retournee",
            _ => "Loc_Statut_EnCours"
        });

    /// <summary>Status is driven by product lines only (services are not returnable).</summary>
    public static StatutBonSortie FromQuantites(IEnumerable<(decimal Quantite, decimal QuantiteRetournee)> productLines)
    {
        var list = productLines as IList<(decimal Quantite, decimal QuantiteRetournee)> ?? productLines.ToList();
        if (list.Count == 0)
            return StatutBonSortie.EnCours;

        var anyOut = list.Any(l => l.Quantite > l.QuantiteRetournee);
        var anyReturned = list.Any(l => l.QuantiteRetournee > 0);
        if (!anyOut)
            return StatutBonSortie.Retournee;
        if (anyReturned)
            return StatutBonSortie.PartiellementRetournee;
        return StatutBonSortie.EnCours;
    }

    public static StatutBonSortie Normalize(StatutBonSortie stored) =>
        stored is StatutBonSortie.EnCours
            or StatutBonSortie.PartiellementRetournee
            or StatutBonSortie.Retournee
            ? stored
            : StatutBonSortie.EnCours;

    public static IBrush ChipBackground(StatutBonSortie s) => s switch
    {
        StatutBonSortie.Retournee => Brush.Parse("#DCFCE7"),
        StatutBonSortie.PartiellementRetournee => Brush.Parse("#FEF3C7"),
        _ => Brush.Parse("#F5E9C8")
    };

    public static IBrush ChipForeground(StatutBonSortie s) => s switch
    {
        StatutBonSortie.Retournee => Brush.Parse("#166534"),
        StatutBonSortie.PartiellementRetournee => Brush.Parse("#92400E"),
        _ => Brush.Parse("#8A7020")
    };

    public static IBrush ChipBorder(StatutBonSortie s) => s switch
    {
        StatutBonSortie.Retournee => Brush.Parse("#86EFAC"),
        StatutBonSortie.PartiellementRetournee => Brush.Parse("#FCD34D"),
        _ => Brush.Parse("#C4A035")
    };
}
