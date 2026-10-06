using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// Codes to put in <c>Observation.code</c> of a PhysicalActivityObservation.
/// </summary>
/// <remarks>
/// The <see href="https://hl7.dk/fhir/hackathons/ValueSet-ActivityCodes.html">ActivityCodes</see> value set is
/// intensional - SNOMED CT "is-a" queries on 418305009, 256235009 and 68130003 plus 129016000 - so it cannot be
/// expanded without a terminology server. These are the members the IG names outright: the three hierarchy roots
/// (an "is-a" filter includes the concept itself), the one enumerated code, and the code the IG example uses.
/// The binding is extensible, so ask the server to expand the value set before offering anything beyond these.
/// </remarks>
public static class ActivityCodes
{
    /// <summary>The code the IG's own PhysicalActivityObservation example uses.</summary>
    public static readonly Coding Walking = Snomed("129006008", "Walking");

    /// <summary>Enumerated directly in the value set.</summary>
    public static readonly Coding ClimbingStairs = Snomed("129016000", "Climbing stairs");

    /// <summary>Root of one of the value set's "is-a" filters.</summary>
    public static readonly Coding SelfPropelledLocomotion = Snomed("418305009", "Self-propelled locomotion activity");

    /// <summary>Root of one of the value set's "is-a" filters.</summary>
    public static readonly Coding Exercise = Snomed("256235009", "Exercise");

    /// <summary>Root of one of the value set's "is-a" filters.</summary>
    public static readonly Coding PhysicalActivity = Snomed("68130003", "Physical activity");

    /// <summary>Every code this app offers, in the order a user is most likely to want them.</summary>
    public static IReadOnlyList<Coding> All { get; } =
        [Walking, ClimbingStairs, Exercise, PhysicalActivity, SelfPropelledLocomotion];

    /// <summary>
    /// Finds the code with the <paramref name="code"/>.
    /// </summary>
    /// <param name="code">The SNOMED CT code to look up.</param>
    /// <returns>The matching coding, or <see langword="null"/> when this app does not offer it.</returns>
    public static Coding? FromCode(string? code)
    {
        return All.FirstOrDefault(coding => coding.Code == code);
    }

    private static Coding Snomed(string code, string display) =>
        new() { System = Telemedicine.SnomedSystem, Code = code, Display = display };
}
