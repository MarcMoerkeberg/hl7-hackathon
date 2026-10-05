using System.Reflection;
using Hl7Hackathon.Framework.Factories.Interfaces;

namespace Hl7Hackathon.UnitTests.Architecture;

[Trait("Category", "Unit")]
public class ArchitectureTests
{
    // Only references the compiler kept are listed, so this catches code that uses a forbidden layer.
    [Theory]
    [InlineData("Hl7Hackathon.Core", "Hl7Hackathon.Services")]
    [InlineData("Hl7Hackathon.Core", "Hl7Hackathon.Fhir")]
    [InlineData("Hl7Hackathon.Core", "Hl7Hackathon.Framework")]
    [InlineData("Hl7Hackathon.Services", "Hl7Hackathon.Fhir")]
    [InlineData("Hl7Hackathon.Services", "Hl7Hackathon.Framework")]
    [InlineData("Hl7Hackathon.Fhir", "Hl7Hackathon.Services")]
    [InlineData("Hl7Hackathon.Fhir", "Hl7Hackathon.Framework")]
    [InlineData("Hl7Hackathon.Framework", "Hl7Hackathon.Web")]
    public void Assembly_DoesNotReferenceAnOuterOrSiblingLayer(string assemblyName, string forbiddenReference)
    {
        // Act
        var references = Assembly.Load(assemblyName).GetReferencedAssemblies().Select(reference => reference.Name);

        // Assert
        Assert.DoesNotContain(forbiddenReference, references);
    }

    [Fact]
    public void FactoryInterfaces_OnlyExposeCoreModels()
    {
        // Arrange
        var factoryInterfaces = typeof(IFhirServerFactory).Assembly.GetExportedTypes()
            .Where(type => type.IsInterface && type.Namespace?.EndsWith(".Factories.Interfaces", StringComparison.Ordinal) == true)
            .ToList();

        // Act
        var violations = factoryInterfaces
            .SelectMany(factoryInterface => factoryInterface.GetMethods())
            .SelectMany(method => method.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .Append(method.ReturnType)
                .Where(IsForbiddenInFactorySignature)
                .Select(type => $"{method.DeclaringType!.Name}.{method.Name} uses {type}"))
            .ToList();

        // Assert
        Assert.NotEmpty(factoryInterfaces);
        Assert.Empty(violations);
    }

    private static bool IsForbiddenInFactorySignature(Type type)
    {
        var isFhirModelOrEntity = type.Namespace is { } typeNamespace
            && (typeNamespace.StartsWith("Hl7Hackathon.Fhir", StringComparison.Ordinal)
                || typeNamespace.StartsWith("Hl7Hackathon.Core.Models.Entities", StringComparison.Ordinal));

        return isFhirModelOrEntity
            || (type.HasElementType && IsForbiddenInFactorySignature(type.GetElementType()!))
            || (type.IsGenericType && type.GetGenericArguments().Any(IsForbiddenInFactorySignature));
    }
}
