using Avalonia.Media;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public static class SoftReservationStatutLabels
{
    public static string Format(ILocaleService locale, StatutReservation s) =>
        locale.T(s switch
        {
            StatutReservation.Brouillon => "SoftRes_Statut_Brouillon",
            StatutReservation.Confirmee => "SoftRes_Statut_Confirmee",
            StatutReservation.Transformee => "SoftRes_Statut_Transformee",
            StatutReservation.Annulee => "SoftRes_Statut_Annulee",
            _ => "SoftRes_Statut_Brouillon"
        });

    public static StatutReservation Normalize(StatutReservation stored) =>
        stored is StatutReservation.Brouillon
            or StatutReservation.Confirmee
            or StatutReservation.Transformee
            or StatutReservation.Annulee
            ? stored
            : StatutReservation.Brouillon;

    public static IBrush ChipBackground(StatutReservation s) => s switch
    {
        StatutReservation.Confirmee => Brush.Parse("#DCFCE7"),
        StatutReservation.Transformee => Brush.Parse("#DBEAFE"),
        StatutReservation.Annulee => Brush.Parse("#FEE2E2"),
        _ => Brush.Parse("#F5E9C8")
    };

    public static IBrush ChipForeground(StatutReservation s) => s switch
    {
        StatutReservation.Confirmee => Brush.Parse("#166534"),
        StatutReservation.Transformee => Brush.Parse("#1E40AF"),
        StatutReservation.Annulee => Brush.Parse("#991B1B"),
        _ => Brush.Parse("#8A7020")
    };

    public static IBrush ChipBorder(StatutReservation s) => s switch
    {
        StatutReservation.Confirmee => Brush.Parse("#86EFAC"),
        StatutReservation.Transformee => Brush.Parse("#93C5FD"),
        StatutReservation.Annulee => Brush.Parse("#FECACA"),
        _ => Brush.Parse("#C4A035")
    };
}
