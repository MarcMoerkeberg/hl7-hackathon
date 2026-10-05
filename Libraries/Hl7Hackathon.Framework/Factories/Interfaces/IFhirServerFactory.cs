using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.Framework.Factories.Interfaces;

public interface IFhirServerFactory
{
    /// <summary>Calls GET [base]/metadata on the configured server; doubles as a connectivity check.</summary>
    Task<Result<FhirServerInfoModel>> GetFhirServerInfoAsync(CancellationToken cancellationToken = default);
}
