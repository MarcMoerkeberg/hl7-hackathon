using Hl7Hackathon.TestHelpers.Fixtures;

namespace Hl7Hackathon.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public class IntegrationTestsCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Integration Tests";
}
