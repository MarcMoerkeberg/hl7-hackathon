using Hl7Hackathon.Core.Models.Entities;

namespace Hl7Hackathon.Services.Interfaces;

public interface IFhirExchangeService
{
    Task<long> InsertFhirExchangeAsync(FhirExchange fhirExchange, CancellationToken cancellationToken = default);

    /// <summary>Returns the newest exchanges without request and response bodies.</summary>
    Task<IReadOnlyList<FhirExchange>> GetRecentFhirExchangesAsync(int limit, CancellationToken cancellationToken = default);

    Task<FhirExchange?> GetFhirExchangeByIdAsync(long fhirExchangeId, CancellationToken cancellationToken = default);

    Task<int> DeleteAllFhirExchangesAsync(CancellationToken cancellationToken = default);
}
