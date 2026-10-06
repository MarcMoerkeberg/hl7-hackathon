namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// One answer the patient gave, as the code of the option they picked.
/// </summary>
public sealed record QuestionnaireAnswerModel
{
    /// <summary>The <see cref="QuestionnaireItemModel.LinkId"/> of the question being answered.</summary>
    public required string LinkId { get; init; }

    /// <summary>The <see cref="AnswerOptionModel.Code"/> of the chosen answer.</summary>
    public required string Code { get; init; }
}
