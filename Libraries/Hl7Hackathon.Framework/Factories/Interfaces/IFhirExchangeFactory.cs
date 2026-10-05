using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.Framework.Factories.Interfaces;

public interface IFhirExchangeFactory
{
    Task<Result<IReadOnlyList<FhirExchangeSummaryModel>>> GetRecentFhirExchangesAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<Result<FhirExchangeModel>> GetFhirExchangeByIdAsync(long fhirExchangeId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the whole exchange log and returns how many rows were removed.</summary>
    Task<Result<int>> ClearFhirExchangesAsync(CancellationToken cancellationToken = default);
}
