namespace GestionCommerciale.Shared.Services;

public interface IDocumentNumberService
{
    Task<string> NextBRAsync(CancellationToken cancellationToken = default);
    Task<string> NextBCAsync(CancellationToken cancellationToken = default);
    Task<string> NextFactureAsync(CancellationToken cancellationToken = default);
    Task<string> NextFactureFournisseurAsync(CancellationToken cancellationToken = default);
    Task<string> NextAvoirFournisseurAsync(CancellationToken cancellationToken = default);
    Task<string> NextLocationAsync(CancellationToken cancellationToken = default);
    Task<string> NextReservationAsync(CancellationToken cancellationToken = default);
}
