namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// One answer a questionnaire allows to a question.
/// </summary>
/// <remarks>The codes come from the questionnaire on the server, never from this app.</remarks>
public sealed record AnswerOptionModel
{
    public required string Code { get; init; }

    /// <summary>The code system the <see cref="Code"/> belongs to.</summary>
    public string? System { get; init; }

    /// <summary>The answer as the questionnaire words it.</summary>
    public string? Display { get; init; }
}
