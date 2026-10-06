using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.QuestionnaireResponses;
using Hl7Hackathon.Fhir.Models.Questionnaires;

namespace Hl7Hackathon.Framework.Mappers;

internal static class QuestionnaireMapper
{
    private const string GroupType = "group";

    public static QuestionnaireModel ToModel(Questionnaire questionnaire) => new()
    {
        // The caller only maps questionnaires the server returned with an id.
        Id = questionnaire.Id!,
        Url = questionnaire.Url,
        Title = questionnaire.Title,
        Items = ToItemModels(questionnaire.Item)
    };

    /// <summary>
    /// Finds the coding the questionnaire publishes for an answer.
    /// </summary>
    /// <param name="questionnaire">The questionnaire being answered.</param>
    /// <param name="linkId">The question that was answered.</param>
    /// <param name="code">The code of the chosen option.</param>
    /// <returns>
    /// The published coding, so the answer carries the server's own system and display, or <see langword="null"/>
    /// when the questionnaire has no such question or no such option on it.
    /// </returns>
    public static Coding? FindAnswerOption(Questionnaire questionnaire, string linkId, string code)
    {
        return FindItem(questionnaire.Item, linkId)
            ?.AnswerOption
            ?.FirstOrDefault(option => option.ValueCoding?.Code == code)
            ?.ValueCoding;
    }

    public static QuestionnaireResponse ToFhir(
        CreateQuestionnaireResponseModel request,
        Questionnaire questionnaire,
        IReadOnlyDictionary<string, Coding> answers,
        string patientReference,
        string episodeOfCareReference) => new()
    {
        Meta = new Meta { Profile = [Telemedicine.Profiles.TelemedicineQuestionnaireResponse] },
        Extension =
        [
            new Extension
            {
                Url = Telemedicine.EpisodeOfCareExtensionUrl,
                ValueReference = new ResourceReference { Reference = episodeOfCareReference }
            }
        ],
        // A response points at the canonical URL, not at "Questionnaire/[id]".
        Questionnaire = request.QuestionnaireUrl,
        Status = QuestionnaireResponseStatus.Completed,
        Subject = new ResourceReference { Reference = patientReference },
        Authored = FhirDates.ToDateTime(request.Authored),
        // The patient answered for themselves.
        Source = new ResourceReference { Reference = patientReference },
        Item = ToResponseItems(questionnaire.Item, answers)
    };

    public static QuestionnaireResponseModel ToModel(QuestionnaireResponse response) => new()
    {
        // The caller only maps a response the server returned with an id.
        Id = response.Id!,
        Status = response.Status.Code,
        Questionnaire = response.Questionnaire,
        Authored = response.Authored,
        PatientReference = response.Subject?.Reference,
        EpisodeOfCareReference = response
            .GetExtensions(Telemedicine.EpisodeOfCareExtensionUrl)
            .FirstOrDefault()?.ValueReference?.Reference,
        AnswerCount = CountAnswers(response.Item)
    };

    private static IReadOnlyList<QuestionnaireItemModel> ToItemModels(IReadOnlyList<QuestionnaireItem>? items)
    {
        // An item with no linkId cannot be answered or referenced, so it is left out rather than shown.
        return items?.Where(item => item.LinkId is not null).Select(item => new QuestionnaireItemModel
        {
            LinkId = item.LinkId!,
            Text = item.Text,
            IsGroup = item.Type == GroupType,
            Options = item.AnswerOption
                ?.Where(option => option.ValueCoding?.Code is not null)
                .Select(option => new AnswerOptionModel
                {
                    Code = option.ValueCoding!.Code!,
                    System = option.ValueCoding.System,
                    Display = option.ValueCoding.Display
                })
                .ToList() ?? [],
            Items = ToItemModels(item.Item)
        }).ToList() ?? [];
    }

    /// <summary>
    /// Mirrors the questionnaire's own item tree, keeping only the branches that lead to an answer.
    /// </summary>
    /// <remarks>FHIR forbids empty arrays, so a group with nothing answered under it is left out entirely.</remarks>
    private static IReadOnlyList<QuestionnaireResponseItem>? ToResponseItems(
        IReadOnlyList<QuestionnaireItem>? items,
        IReadOnlyDictionary<string, Coding> answers)
    {
        var responseItems = new List<QuestionnaireResponseItem>();

        foreach (var item in items ?? [])
        {
            if (item.LinkId is not { } linkId)
            {
                continue;
            }

            var nested = ToResponseItems(item.Item, answers);
            var answer = answers.GetValueOrDefault(linkId);
            if (answer is null && nested is null)
            {
                continue;
            }

            responseItems.Add(new QuestionnaireResponseItem
            {
                LinkId = linkId,
                Text = item.Text,
                Answer = answer is null ? null : [new QuestionnaireResponseAnswer { ValueCoding = answer }],
                Item = nested
            });
        }

        return responseItems.Count == 0 ? null : responseItems;
    }

    private static QuestionnaireItem? FindItem(IReadOnlyList<QuestionnaireItem>? items, string linkId)
    {
        foreach (var item in items ?? [])
        {
            if (item.LinkId == linkId)
            {
                return item;
            }

            if (FindItem(item.Item, linkId) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    private static int CountAnswers(IReadOnlyList<QuestionnaireResponseItem>? items)
    {
        return (items ?? []).Sum(item => (item.Answer?.Count ?? 0) + CountAnswers(item.Item));
    }
}
