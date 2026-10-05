using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Validation;
using Hl7Hackathon.TestHelpers;

namespace Hl7Hackathon.UnitTests.Fhir.Validation;

[Trait("Category", "Unit")]
public class FhirModelValidatorTests
{
    [Fact]
    public void Validate_ReturnsNoViolations_ForResourcesThatMeetTheIg()
    {
        // Arrange
        Resource[] resources =
        [
            TestFhirResources.TelemedicinePatient(),
            TestFhirResources.TelemedicineEpisodeOfCare(),
            TestFhirResources.SleepObservation(),
            TestFhirResources.PhysicalActivityObservation(),
            TestFhirResources.TelemedicineQuestionnaireResponse()
        ];

        // Act & Assert
        Assert.All(resources, resource => Assert.Empty(FhirModelValidator.Validate(resource)));
    }

    [Fact]
    public void Validate_ReportsTheMissingProfile_ForEveryIgResource()
    {
        // Arrange
        Resource[] resources =
        [
            TestFhirResources.TelemedicinePatient() with { Meta = null },
            TestFhirResources.TelemedicineEpisodeOfCare() with { Meta = null },
            TestFhirResources.SleepObservation() with { Meta = null },
            TestFhirResources.TelemedicineQuestionnaireResponse() with { Meta = null }
        ];

        // Act & Assert
        Assert.All(resources, resource =>
        {
            var violation = Assert.Single(FhirModelValidator.Validate(resource));
            Assert.StartsWith($"{resource.GetType().Name}.meta: meta.profile must include", violation);
        });
    }

    [Fact]
    public void Validate_ReportsTheElementPath_OfANestedViolation()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with { EffectivePeriod = new Period { Start = "06-10-2026" } };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.StartsWith("Observation.effectivePeriod.start:", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ReportsAttributeAndProfileViolations_Together()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with { Subject = null, Extension = null };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.Equal(2, violations.Count);
        Assert.Contains(violations, violation => violation.StartsWith("Observation.subject:", StringComparison.Ordinal));
        Assert.Contains(violations, violation => violation.StartsWith("Observation.extension:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsAnEmptyList_BecauseFhirForbidsEmptyArrays()
    {
        // Arrange
        var patient = TestFhirResources.TelemedicinePatient() with { Name = [] };

        // Act
        var violations = FhirModelValidator.Validate(patient);

        // Assert
        Assert.StartsWith("Patient.name:", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ReportsAPerformerCountOtherThanOne()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with
        {
            Performer = [new ResourceReference { Reference = "Patient/1" }, new ResourceReference { Reference = "Patient/2" }]
        };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.StartsWith("Observation.performer:", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ReportsAnObservation_WithoutTheEpisodeOfCareExtension()
    {
        // Arrange
        var observation = TestFhirResources.PhysicalActivityObservation() with
        {
            Extension = [new Extension { Url = "http://example.org/other-extension" }]
        };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.Contains("workflow-episodeOfCare", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ReportsEffectiveDateTime_BecauseTheProfilesOnlyAllowAPeriod()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with { EffectiveDateTime = "2026-10-06" };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.StartsWith("Observation.effectiveDateTime:", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ReportsASleepObservation_WithoutTheSleepCode()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with { Code = new CodeableConcept { Text = "Sleep" } };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.Contains("248263006", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ReportsASleepObservation_WithoutAValue()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with { ValueQuantity = null };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.StartsWith("Observation.valueQuantity:", Assert.Single(violations));
    }

    [Fact]
    public void Validate_AllowsAPhysicalActivityObservation_WithoutAValue()
    {
        // Arrange
        var observation = TestFhirResources.PhysicalActivityObservation() with { ValueQuantity = null };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Validate_ReportsTheWrongUnit_ForEachKindOfObservation()
    {
        // Arrange
        var sleep = TestFhirResources.SleepObservation() with { ValueQuantity = new Quantity { Value = 7, Unit = "Minutes" } };
        var activity = TestFhirResources.PhysicalActivityObservation() with { ValueQuantity = new Quantity { Value = 45, Unit = "Hours" } };

        // Act
        var sleepViolation = Assert.Single(FhirModelValidator.Validate(sleep));
        var activityViolation = Assert.Single(FhirModelValidator.Validate(activity));

        // Assert
        Assert.Contains("\"Hours\"", sleepViolation);
        Assert.Contains("\"Minutes\"", activityViolation);
    }

    [Fact]
    public void Validate_ReportsAUnitCodeWithoutASystem()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with
        {
            ValueQuantity = new Quantity { Value = 7, Unit = "Hours", Code = "h" }
        };

        // Act
        var violations = FhirModelValidator.Validate(observation);

        // Assert
        Assert.StartsWith("Observation.valueQuantity.system:", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ReportsATransactionEntry_WithoutARequest()
    {
        // Arrange
        var transaction = new Bundle
        {
            Type = BundleType.Transaction,
            Entry = [new BundleEntry { Resource = TestFhirResources.TelemedicinePatient() }]
        };

        // Act
        var violations = FhirModelValidator.Validate(transaction);

        // Assert
        Assert.StartsWith("Bundle.entry:", Assert.Single(violations));
    }

    [Fact]
    public void Validate_ValidatesTheResourcesInsideABundle()
    {
        // Arrange
        var transaction = new Bundle
        {
            Type = BundleType.Transaction,
            Entry =
            [
                new BundleEntry
                {
                    Resource = TestFhirResources.TelemedicineEpisodeOfCare() with { Period = null },
                    Request = new BundleEntryRequest { Method = HttpVerb.Post, Url = "EpisodeOfCare" }
                }
            ]
        };

        // Act
        var violations = FhirModelValidator.Validate(transaction);

        // Assert
        Assert.StartsWith("Bundle.entry[0].resource.period:", Assert.Single(violations));
    }
}
