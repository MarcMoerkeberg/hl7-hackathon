using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Hl7Hackathon.Fhir.Models;

namespace Hl7Hackathon.Fhir.Validation;

/// <summary>
/// Validates a FHIR resource against the data annotations and <see cref="IValidatableObject"/> rules of its models.
/// </summary>
/// <remarks><see cref="Validator"/> only checks the object it is given, so this walks the nested elements itself.</remarks>
internal static class FhirModelValidator
{
    /// <summary>
    /// Validates the <paramref name="resource"/> and every element nested in it, including the resources of bundle entries.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <returns>One message per violation, prefixed with the FHIR path of the element (eg. "Observation.subject").</returns>
    public static IReadOnlyList<string> Validate(Resource resource)
    {
        var errors = new List<string>();
        ValidateElement(resource, resource.GetType().Name, errors);

        return errors;
    }

    private static void ValidateElement(object element, string path, List<string> errors)
    {
        var results = new List<ValidationResult>();
        var properties = element.GetType().GetProperties();

        foreach (var property in properties)
        {
            Validator.TryValidateProperty(property.GetValue(element), new ValidationContext(element) { MemberName = property.Name }, results);
        }

        // Validator.TryValidateObject skips these rules when an attribute fails; running them here reports everything at once.
        if (element is IValidatableObject validatable)
        {
            results.AddRange(validatable.Validate(new ValidationContext(element)));
        }

        errors.AddRange(results.Select(result => $"{ToElementPath(path, result.MemberNames.FirstOrDefault())}: {result.ErrorMessage}"));

        foreach (var property in properties)
        {
            var propertyPath = ToElementPath(path, property.Name);
            switch (property.GetValue(element))
            {
                case IEnumerable<object> items:
                    var index = 0;
                    foreach (var item in items)
                    {
                        if (IsFhirModel(item))
                        {
                            ValidateElement(item, $"{propertyPath}[{index}]", errors);
                        }

                        index++;
                    }

                    break;
                case { } child when IsFhirModel(child):
                    ValidateElement(child, propertyPath, errors);
                    break;
            }
        }
    }

    private static bool IsFhirModel(object value)
    {
        return value.GetType().Namespace?.StartsWith(typeof(Resource).Namespace!, StringComparison.Ordinal) == true;
    }

    private static string ToElementPath(string path, string? propertyName)
    {
        return propertyName is null
            ? path
            : $"{path}.{JsonNamingPolicy.CamelCase.ConvertName(propertyName)}";
    }
}
