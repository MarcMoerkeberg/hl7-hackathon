using System.Text.Json;
using System.Text.Json.Serialization;
using Hl7Hackathon.Fhir.Codes;

namespace Hl7Hackathon.Fhir.Common;

/// <summary>
/// Reads and writes every <see cref="FhirCode{TSelf}"/> as its bare code string, eg. <c>"status": "final"</c>.
/// </summary>
internal sealed class FhirCodeJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.BaseType is { IsGenericType: true } baseType
            && baseType.GetGenericTypeDefinition() == typeof(FhirCode<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(FhirCodeJsonConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class FhirCodeJsonConverter<TCode> : JsonConverter<TCode>
        where TCode : FhirCode<TCode>
    {
        public override TCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var code = reader.GetString();

            // A JsonException is what FhirService treats as "the body does not fit our model".
            return code is not null && FhirCode<TCode>.TryFromCode(code, out var value)
                ? value
                : throw new JsonException($"'{code}' is not a code in {typeof(TCode).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TCode value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Code);
        }
    }
}
