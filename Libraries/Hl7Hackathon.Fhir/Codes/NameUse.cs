namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// The use of a human name.
/// </summary>
/// <remarks>Value set: <see href="https://hl7.org/fhir/R4/valueset-name-use.html">name-use (R4)</see>.</remarks>
public sealed record NameUse : FhirCode<NameUse>
{
    /// <summary>Known as/conventional/the one you normally use.</summary>
    public static readonly NameUse Usual = new("usual");

    /// <summary>The formal name as registered in an official (government) registry, but which name might not be commonly used.</summary>
    public static readonly NameUse Official = new("official");

    /// <summary>A temporary name.</summary>
    public static readonly NameUse Temp = new("temp");

    /// <summary>A name that is used to address the person in an informal manner, but is not part of their formal or usual name.</summary>
    public static readonly NameUse Nickname = new("nickname");

    /// <summary>Anonymous assigned name, alias, or pseudonym (used to protect a person's identity for privacy reasons).</summary>
    public static readonly NameUse Anonymous = new("anonymous");

    /// <summary>This name is no longer in use (or was never correct, but retained for records).</summary>
    public static readonly NameUse Old = new("old");

    /// <summary>A name used prior to changing name because of marriage.</summary>
    public static readonly NameUse Maiden = new("maiden");

    private NameUse(string code)
        : base(code)
    {
    }
}
