namespace GestionCommerciale.Modules.Reservation.Models;

/// <summary>Soft-booking reservation status (no stock movement).</summary>
public enum StatutReservation
{
    Brouillon = 0,
    Confirmee = 1,
    Transformee = 2,
    Annulee = 3
}
