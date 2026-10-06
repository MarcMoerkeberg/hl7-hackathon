using System.Text.Json;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.Questionnaires;
using Hl7Hackathon.TestHelpers;

namespace Hl7Hackathon.UnitTests.Fhir;

[Trait("Category", "Unit")]
public class FhirJsonSerializerTests
{
    [Fact]
    public void Serialize_WritesResourceTypeFirst_ThenTheBaseElements()
    {
        // Arrange
        var patient = TestFhirResources.TelemedicinePatient() with { Id = "1" };

        // Act
        var json = FhirJsonSerializer.Serialize(patient);

        // Assert
        var propertyNames = ReadPropertyNames(json);
        Assert.Equal(["resourceType", "id", "meta"], propertyNames.Take(3));
        Assert.Contains("\"resourceType\": \"Patient\"", json);
    }

    [Fact]
    public void Serialize_LeavesOutNullElements()
    {
        // Arrange
        var patient = TestFhirResources.TelemedicinePatient() with { BirthDate = null, Gender = null };

        // Act
        var json = FhirJsonSerializer.Serialize(patient);

        // Assert
        Assert.DoesNotContain("birthDate", json);
        Assert.DoesNotContain("gender", json);
    }

    [Fact]
    public void Serialize_WritesCodesAsTheirBareCode()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation();

        // Act
        var json = FhirJsonSerializer.Serialize(observation);

        // Assert
        Assert.Contains("\"status\": \"final\"", json);
    }

    [Fact]
    public void Serialize_WritesChoiceElementsWithTheirTypeSuffix()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation();

        // Act
        var propertyNames = ReadPropertyNames(FhirJsonSerializer.Serialize(observation));

        // Assert
        Assert.Contains("effectivePeriod", propertyNames);
        Assert.Contains("valueQuantity", propertyNames);
    }

    [Fact]
    public void Deserialize_CreatesTheTypeNamedByResourceType_ForEveryBundleEntry()
    {
        // Arrange
        const string json = """
            {
              "resourceType": "Bundle",
              "type": "searchset",
              "entry": [
                { "resource": { "resourceType": "Patient", "id": "1" } },
                { "resource": { "resourceType": "Observation", "id": "2", "status": "final", "code": {} } }
              ]
            }
            """;

        // Act
        var bundle = FhirJsonSerializer.Deserialize<Bundle>(json);

        // Assert
        Assert.Equal(BundleType.Searchset, bundle.Type);
        Assert.Collection(
            bundle.Entry!,
            entry => Assert.IsType<Patient>(entry.Resource),
            entry => Assert.IsType<Observation>(entry.Resource));
    }

    [Fact]
    public void Deserialize_ReadsResourceType_WhenItIsNotTheFirstProperty()
    {
        // Arrange
        const string json = """{ "id": "1", "gender": "female", "resourceType": "Patient" }""";

        // Act
        var resource = FhirJsonSerializer.Deserialize(json);

        // Assert
        var patient = Assert.IsType<Patient>(resource);
        Assert.Equal(AdministrativeGender.Female, patient.Gender);
    }

    [Fact]
    public void Deserialize_ReturnsAPlainResource_WhenResourceTypeIsNotModelled()
    {
        // Arrange
        const string json = """{ "resourceType": "Practitioner", "id": "9" }""";

        // Act
        var resource = FhirJsonSerializer.Deserialize(json);

        // Assert
        Assert.Equal(typeof(Resource), resource.GetType());
        Assert.Equal("9", resource.Id);
    }

    [Fact]
    public void Deserialize_ReturnsTheDeclaredCodeInstance()
    {
        // Arrange
        const string json = """{ "resourceType": "Observation", "status": "final", "code": {} }""";

        // Act
        var observation = FhirJsonSerializer.Deserialize<Observation>(json);

        // Assert
        Assert.Same(ObservationStatus.Final, observation.Status);
    }

    [Fact]
    public void Deserialize_Throws_WhenACodeIsNotInItsValueSet()
    {
        // Arrange
        const string json = """{ "resourceType": "Observation", "status": "Final", "code": {} }""";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => FhirJsonSerializer.Deserialize(json));
        Assert.Contains("'Final' is not a code in ObservationStatus", exception.Message);
    }

    [Fact]
    public void Deserialize_ReadsAQuestionnaire_WhenAnotherSystemLeftOutElementsR4Requires()
    {
        // Arrange
        // A shared server holds other people's questionnaires; one of them missing status, item.linkId or item.type
        // must not fail the whole search.
        const string json = """
            {
              "resourceType": "Bundle",
              "type": "searchset",
              "entry": [
                {
                  "resource": {
                    "resourceType": "Questionnaire",
                    "id": "someone-elses",
                    "item": [ { "text": "A question with no linkId or type" } ]
                  }
                }
              ]
            }
            """;

        // Act
        var bundle = FhirJsonSerializer.Deserialize<Bundle>(json);

        // Assert
        var questionnaire = Assert.IsType<Questionnaire>(Assert.Single(bundle.Entry!).Resource);
        Assert.Equal("someone-elses", questionnaire.Id);
        Assert.Null(questionnaire.Status);
        Assert.Null(Assert.Single(questionnaire.Item!).LinkId);
    }

    [Fact]
    public void Deserialize_Throws_WhenARequiredElementIsMissing()
    {
        // Arrange
        const string json = """{ "resourceType": "Observation", "code": {} }""";

        // Act & Assert
        Assert.Throws<JsonException>(() => FhirJsonSerializer.Deserialize(json));
    }

    private static List<string> ReadPropertyNames(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.EnumerateObject().Select(property => property.Name).ToList();
    }
}
