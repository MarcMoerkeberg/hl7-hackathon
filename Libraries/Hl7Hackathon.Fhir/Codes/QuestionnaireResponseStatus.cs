namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// The lifecycle status of a questionnaire response.
/// </summary>
/// <remarks>Value set: <see href="https://hl7.org/fhir/R4/valueset-questionnaire-answers-status.html">questionnaire-answers-status (R4)</see>.</remarks>
public sealed record QuestionnaireResponseStatus : FhirCode<QuestionnaireResponseStatus>
{
    /// <summary>This QuestionnaireResponse has been partially filled out with answers but changes or additions are still expected to be made to it.</summary>
    public static readonly QuestionnaireResponseStatus InProgress = new("in-progress");

    /// <summary>This QuestionnaireResponse has been filled out with answers and the current content is regarded as definitive.</summary>
    public static readonly QuestionnaireResponseStatus Completed = new("completed");

    /// <summary>This QuestionnaireResponse has been filled out with answers, then marked as complete, yet changes or additions have been made to it afterwards.</summary>
    public static readonly QuestionnaireResponseStatus Amended = new("amended");

    /// <summary>This QuestionnaireResponse was entered in error and voided.</summary>
    public static readonly QuestionnaireResponseStatus EnteredInError = new("entered-in-error");

    /// <summary>This QuestionnaireResponse has been partially filled out with answers but has been abandoned.</summary>
    public static readonly QuestionnaireResponseStatus Stopped = new("stopped");

    private QuestionnaireResponseStatus(string code)
        : base(code)
    {
    }
}
