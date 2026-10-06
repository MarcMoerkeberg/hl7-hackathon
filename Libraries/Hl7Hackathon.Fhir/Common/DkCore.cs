namespace Hl7Hackathon.Fhir.Common;

/// <summary>
/// Identifier systems from DK Core, which the telemedicine IG builds on.
/// </summary>
/// <remarks>See <see href="https://hl7.dk/fhir/core/">DK Core</see>.</remarks>
public static class DkCore
{
    /// <summary>
    /// The system that names a Danish CPR number in <c>Identifier.system</c>.
    /// </summary>
    /// <remarks>See <see href="https://hl7.dk/fhir/core/StructureDefinition-dk-core-patient.html">DkCorePatient</see>.</remarks>
    public const string CprSystem = "urn:oid:1.2.208.176.1.2";
}
