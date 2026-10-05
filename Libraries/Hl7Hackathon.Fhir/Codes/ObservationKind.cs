using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// The kinds of Observation in the telemedicine IG, with the profile, code and unit each of them fixes.
/// </summary>
/// <remarks>Not a wire value: an Observation says which kind it is through <c>meta.profile</c>.</remarks>
public sealed record ObservationKind
{
    /// <summary>Daily hours of sleep.</summary>
    /// <remarks>Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-SleepObservation.html">SleepObservation</see>.</remarks>
    public static readonly ObservationKind Sleep = new(
        Telemedicine.Profiles.SleepObservation,
        new Coding { System = Telemedicine.SnomedSystem, Code = "248263006", Display = "Duration of sleep" },
        "Hours",
        isValueRequired: true);

    /// <summary>Daily minutes of physical activity.</summary>
    /// <remarks>
    /// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-PhysicalActivityObservation.html">PhysicalActivityObservation</see>.
    /// The code is chosen from the <see href="https://hl7.dk/fhir/hackathons/ValueSet-ActivityCodes.html">ActivityCodes</see>
    /// value set, eg. SNOMED CT 129006008 "Walking".
    /// </remarks>
    public static readonly ObservationKind PhysicalActivity = new(
        Telemedicine.Profiles.PhysicalActivityObservation,
        null,
        "Minutes",
        isValueRequired: false);

    private ObservationKind(string profile, Coding? code, string unit, bool isValueRequired)
    {
        Profile = profile;
        Code = code;
        Unit = unit;
        IsValueRequired = isValueRequired;
    }

    /// <summary>Every kind of Observation in the IG.</summary>
    public static IReadOnlyList<ObservationKind> All { get; } = [Sleep, PhysicalActivity];

    /// <summary>The canonical URL of the profile, to put in <c>meta.profile</c>.</summary>
    public string Profile { get; }

    /// <summary>The code the profile fixes in <c>Observation.code</c>, or <see langword="null"/> when it is chosen from a value set.</summary>
    public Coding? Code { get; }

    /// <summary>The fixed <c>valueQuantity.unit</c>.</summary>
    public string Unit { get; }

    /// <summary>Whether the profile requires <c>valueQuantity</c>.</summary>
    public bool IsValueRequired { get; }

    /// <summary>
    /// Finds the kind whose profile the <paramref name="resource"/> claims in <c>meta.profile</c>.
    /// </summary>
    /// <param name="resource">The resource to look at.</param>
    /// <returns>The claimed kind, or <see langword="null"/> when the resource claims none of them.</returns>
    public static ObservationKind? FromClaimedProfile(Resource resource)
    {
        return All.FirstOrDefault(kind => resource.ClaimsProfile(kind.Profile));
    }
}
