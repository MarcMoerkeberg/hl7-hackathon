using Hl7Hackathon.TestHelpers.Fixtures;

namespace Hl7Hackathon.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public class FhirServerTestsCollection : ICollectionFixture<FhirServerFixture>
{
    public const string Name = "FHIR Server Tests";
}
