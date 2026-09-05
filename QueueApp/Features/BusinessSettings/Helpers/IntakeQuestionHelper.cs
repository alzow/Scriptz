using QueueApp.Features.BusinessSettings.Constants;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Features.BusinessSettings.Helpers;

// The rules a question list has to obey, and the sentences that explain them to the owner. A
// conditional question may only point at a question above it, which is what makes reordering a
// validated move rather than a swap.
public static class IntakeQuestionHelper
{
    private const string ConditionPrefix = "Only when";
    private const string ValueJoiner = " or ";
    private static readonly char[] PromptPunctuation = { '?', ':', '.', ' ' };

    // "Only when Medical aid or cash is Medical aid" — the parent's prompt reads as a noun here, so
    // its trailing question mark would make the sentence stutter.
    public static string ConditionSentence(IntakeFieldResponse field, IEnumerable<IntakeFieldResponse> allFields)
    {
        if (field.VisibilityRule is not { } rule)
            return string.Empty;

        var parent = allFields.FirstOrDefault(f => f.Id == rule.FieldId);
        if (parent is null)
            return string.Empty;

        var values = string.Join(ValueJoiner, rule.Values);
        return $"{ConditionPrefix} {TrimPrompt(parent.Label)} {IntakeQuestionConstants.RuleJoiner} {values}";
    }

    public static string TrimPrompt(string prompt) => prompt.TrimEnd(PromptPunctuation);

    // "4 questions, 3 required".
    public static string SummaryLine(IReadOnlyCollection<IntakeFieldResponse> fields)
    {
        var required = fields.Count(f => f.IsRequired);
        var questions = fields.Count == 1 ? "1 question" : $"{fields.Count} questions";
        var requiredText = required == 0 ? "none required" : $"{required} required";
        return $"{questions}, {requiredText}";
    }

    // "Adds about 40 seconds to joining". Rounded to five so it reads as the estimate it is.
    public static string CostLine(int questionCount)
    {
        var seconds = questionCount * BusinessSettingsConstants.SecondsPerQuestion;
        var rounded = (int)(Math.Round(seconds / 5m) * 5);
        return $"Adds about {rounded} seconds to joining";
    }

    // Every question whose rule points at this one. Deleting it, or removing the option they match
    // on, silently turns each of these into a question everyone gets asked.
    public static List<IntakeFieldResponse> Dependants(Guid fieldId, IEnumerable<IntakeFieldResponse> allFields) =>
        allFields.Where(f => f.VisibilityRule?.FieldId == fieldId).ToList();

    public static List<IntakeFieldResponse> DependantsOnOption(
        Guid fieldId, string option, IEnumerable<IntakeFieldResponse> allFields) =>
        allFields
            .Where(f => f.VisibilityRule?.FieldId == fieldId)
            .Where(f => f.VisibilityRule!.Values.Contains(option))
            .ToList();

    public static string NameList(IEnumerable<IntakeFieldResponse> fields) =>
        string.Join(", ", fields.Select(f => TrimPrompt(f.Label)));

    public static string OptionRemovalWarning(string option, IEnumerable<IntakeFieldResponse> dependants) =>
        $"Removing \"{option}\" will make {NameList(dependants)} ask everyone instead.";

    public static string DeleteWarning(IEnumerable<IntakeFieldResponse> dependants) =>
        $"{NameList(dependants)} only shows when this one is answered, and will start being asked " +
        $"of everyone. {IntakeQuestionConstants.DeleteConfirmMessage}";

    // Null when the move is allowed; otherwise the reason it isn't, naming both questions. Works on
    // the order the list would have after the move rather than on sort_order, because a list
    // written by an older build can carry duplicate sort_order values.
    public static string? DescribeBrokenOrder(
        IReadOnlyList<IntakeFieldResponse> ordered, int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= ordered.Count || toIndex >= ordered.Count)
            return null;

        var moved = ordered.ToList();
        var field = moved[fromIndex];
        moved.RemoveAt(fromIndex);
        moved.Insert(toIndex, field);

        var position = moved
            .Select((f, index) => (f.Id, index))
            .ToDictionary(pair => pair.Id, pair => pair.index);

        foreach (var candidate in moved)
        {
            if (candidate.VisibilityRule is not { } rule)
                continue;

            if (!position.TryGetValue(rule.FieldId, out var parentIndex))
                continue;

            if (parentIndex < position[candidate.Id])
                continue;

            var parent = moved.First(f => f.Id == rule.FieldId);
            return $"{TrimPrompt(candidate.Label)} only shows when {TrimPrompt(parent.Label)} " +
                   $"has been answered, so {TrimPrompt(parent.Label)} has to stay above it.";
        }

        return null;
    }
}
