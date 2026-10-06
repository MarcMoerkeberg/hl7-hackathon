using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Framework.Mappers;
using FhirPatient = Hl7Hackathon.Fhir.Models.Patient;
using PatientEntity = Hl7Hackathon.Core.Models.Entities.Patient;

namespace Hl7Hackathon.UnitTests.Mappers;

[Trait("Category", "Unit")]
public class PatientMapperTests
{
    [Fact]
    public void ToModel_PrefersTheOfficialName()
    {
        // Arrange
        var patient = new FhirPatient
        {
            Id = "7",
            Name =
            [
                new HumanName { Use = NameUse.Nickname, Family = "B", Given = ["Nan"] },
                new HumanName { Use = NameUse.Official, Family = "Berggren", Given = ["Nancy", "Ann"] }
            ]
        };

        // Act
        var model = PatientMapper.ToModel(patient);

        // Assert
        Assert.Equal("Nancy Ann", model.GivenName);
        Assert.Equal("Berggren", model.FamilyName);
    }

    [Fact]
    public void ToModel_FallsBackToTheFirstName_WhenNoneIsOfficial()
    {
        // Arrange
        var patient = new FhirPatient
        {
            Id = "7",
            Name = [new HumanName { Use = NameUse.Usual, Family = "Berggren", Given = ["Nancy"] }]
        };

        // Act
        var model = PatientMapper.ToModel(patient);

        // Assert
        Assert.Equal("Nancy", model.GivenName);
    }

    [Fact]
    public void ToModel_TakesTheCprIdentifier_AndIgnoresOtherSystems()
    {
        // Arrange
        var patient = new FhirPatient
        {
            Id = "7",
            Identifier =
            [
                new Identifier { System = "urn:oid:1.2.3", Value = "other-system" },
                new Identifier { System = DkCore.CprSystem, Value = "2512489996" }
            ]
        };

        // Act
        var model = PatientMapper.ToModel(patient);

        // Assert
        Assert.Equal("2512489996", model.Cpr);
    }

    [Fact]
    public void ToModel_LeavesEverythingButTheIdNull_WhenTheServerHoldsNothingElse()
    {
        // Arrange
        var patient = new FhirPatient { Id = "7" };

        // Act
        var model = PatientMapper.ToModel(patient);

        // Assert
        Assert.Equal("7", model.Id);
        Assert.Null(model.Cpr);
        Assert.Null(model.GivenName);
        Assert.Null(model.FamilyName);
        Assert.Null(model.Gender);
        Assert.Null(model.BirthDate);
    }

    [Fact]
    public void ToFhir_SendsAnUnknownGender_WhenTheRecordHoldsTextOutsideTheValueSet()
    {
        // Arrange
        var patient = CreateEntity();
        patient.Gender = "kvinde";

        // Act
        var fhirPatient = PatientMapper.ToFhir(patient);

        // Assert
        Assert.Same(AdministrativeGender.Unknown, fhirPatient.Gender);
    }

    [Fact]
    public void ToCprSearch_SearchesTheDkCoreIdentifierSystem()
    {
        // Act
        var search = PatientMapper.ToCprSearch(CreateEntity());

        // Assert
        Assert.Equal($"identifier={DkCore.CprSystem}|2512489996", search);
    }

    [Fact]
    public void ToSearchParameters_StripsTheDashFromACprNumber()
    {
        // Act
        var parameters = PatientMapper.ToSearchParameters(new SearchPatientsModel { Cpr = "251248-9996" });

        // Assert
        Assert.Equal(("identifier", $"{DkCore.CprSystem}|2512489996"), Assert.Single(parameters));
    }

    [Fact]
    public void ToSearchParameters_ReturnsNothing_WhenTheRequestHoldsNoCriterion()
    {
        // Act
        var parameters = PatientMapper.ToSearchParameters(new SearchPatientsModel { Cpr = " - ", Name = "  " });

        // Assert
        Assert.Empty(parameters);
    }

    private static PatientEntity CreateEntity() => new()
    {
        Id = 1,
        Cpr = "2512489996",
        GivenName = "Nancy Ann",
        FamilyName = "Berggren",
        Gender = "female",
        BirthDate = new DateTime(1948, 12, 25)
    };
}
