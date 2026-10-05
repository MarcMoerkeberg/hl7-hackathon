using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// A FHIR <c>code</c> from a required value set, as a closed set of instances.
/// </summary>
/// <remarks>
/// Each value set derives from this and declares its codes as <c>public static readonly</c> fields. On the wire it
/// is the bare code string, see <see cref="Common.FhirCodeJsonConverterFactory"/>.
/// </remarks>
/// <typeparam name="TSelf">The value set deriving from this.</typeparam>
public abstract record FhirCode<TSelf>
    where TSelf : FhirCode<TSelf>
{
    private static readonly Lazy<Dictionary<string, TSelf>> _codes = new(() => typeof(TSelf)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(TSelf))
        .Select(field => (TSelf)field.GetValue(null)!)
        .ToDictionary(code => code.Code, StringComparer.Ordinal));

    protected FhirCode(string code)
    {
        Code = code;
    }

    /// <summary>The code as it is written in FHIR JSON; FHIR codes are case-sensitive.</summary>
    public string Code { get; }

    /// <summary>Every code in the value set.</summary>
    public static IReadOnlyCollection<TSelf> All => _codes.Value.Values;

    /// <summary>
    /// Finds the <typeparamref name="TSelf"/> with the <paramref name="code"/>.
    /// </summary>
    /// <param name="code">The code to look up.</param>
    /// <param name="value">The matching instance, or <see langword="null"/> when the value set has no such code.</param>
    /// <returns><see langword="true"/> when the value set has the code.</returns>
    public static bool TryFromCode(string code, [NotNullWhen(true)] out TSelf? value)
    {
        return _codes.Value.TryGetValue(code, out value);
    }

    public sealed override string ToString() => Code;
}
