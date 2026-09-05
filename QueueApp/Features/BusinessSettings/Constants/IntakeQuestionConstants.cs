namespace QueueApp.Features.BusinessSettings.Constants;

public static class IntakeQuestionConstants
{
    public const string AddTitle = "Add question";
    public const string EditTitle = "Edit question";

    public const string PromptLabel = "What you're asking";
    public const string PromptPlaceholder = "The question, as the customer sees it";
    public const string PromptHint = "The customer sees this exactly as you write it.";
    public const string PromptRequiredError = "The question is required.";

    public const string HintLabel = "Hint under the field (optional)";
    public const string HintPlaceholder = "e.g. CA 418 772";
    public const string HintExplainer = "An example here prevents most of the answers you'd have to phone about.";

    public const string KindLabel = "Kind of answer";

    public const string RequiredToggleTitle = "They must answer";
    public const string RequiredToggleSubtitle = "They can't join without it";

    public const string OptionsLabel = "The options";
    public const string AddOptionText = "Add an option";
    public const string NewOptionPlaceholder = "Add a choice";
    public const string OptionsTooFew = "Add at least two choices.";

    public const string WhenToAskLabel = "When to ask";
    public const string AlwaysText = "Always";
    public const string ConditionalText = "Only when an earlier answer matches";
    public const string RuleLead = "Ask this only if";
    public const string RuleJoiner = "is";
    public const string RuleOrderNote =
        "Only questions above this one can be used. Drag to reorder if you need a different one.";
    public const string RulePickQuestionPlaceholder = "Pick a question";
    public const string RulePickValuePlaceholder = "Pick an answer";

    public const string SaveText = "Save question";
    public const string SavingText = "Saving…";
    public const string DeleteText = "Remove this question";
    public const string DeletingText = "Removing…";

    // §5 of the spec: this one isn't only a UX concern. Storage, RLS and retention for these files
    // are specified in Documentation/service-intake-fields-backend-requirements.md.
    public const string FileWarningTitle = "Files are personal information.";
    public const string FileWarningBody =
        "They're stored with the visit and anyone who can open your board can see them. " +
        "Ask for documents only when you truly need them.";

    public const string OptionsNeededTitle = "Needs choices";
    public const string OptionsNeededMessage =
        "A select question needs at least two choices for the customer to pick from.";

    public const string ConditionNeededTitle = "Needs a condition";
    public const string ConditionNeededMessage =
        "Pick which earlier question, and which answer to it, should make this one show up.";

    public const string DeleteConfirmTitle = "Remove this question?";
    public const string DeleteConfirmMessage =
        "New customers won't be asked it. Answers already given stay on the visits that gave them.";
    public const string DeleteConfirmAccept = "Remove";
    public const string DeleteConfirmCancel = "Keep it";

    // Naming what breaks is the whole point — "this may affect other questions" tells an owner
    // nothing they can act on.
    public const string DeleteDependantsTitle = "Other questions depend on this one";
    public const string DeleteDependantsAccept = "Remove anyway";

    public const string OptionDependantsTitle = "One later question depends on this";
    public const string OptionDependantsAccept = "Remove the option";
    public const string OptionDependantsCancel = "Keep it";

    public const string ReorderBlockedTitle = "Can't move it there";
}
