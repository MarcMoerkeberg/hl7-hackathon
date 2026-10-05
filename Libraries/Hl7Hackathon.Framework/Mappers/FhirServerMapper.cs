using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Models.CapabilityStatements;

namespace Hl7Hackathon.Framework.Mappers;

internal static class FhirServerMapper
{
    public static FhirServerInfoModel ToInfoModel(ServerConnection server, CapabilityStatement capabilityStatement) => new()
    {
        Name = server.Name,
        BaseUrl = server.BaseUrl,
        SoftwareName = capabilityStatement.Software?.Name,
        SoftwareVersion = capabilityStatement.Software?.Version,
        FhirVersion = capabilityStatement.FhirVersion,
        SupportedResourceTypes = (capabilityStatement.Rest ?? [])
            .SelectMany(rest => rest.Resource ?? [])
            .Select(resource => resource.Type)
            .Distinct()
            .Order()
            .ToList()
    };
}
