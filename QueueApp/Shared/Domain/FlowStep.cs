namespace QueueApp.Shared.Domain;

public enum FlowStep
{
    Operator,
    Service,

    // Only ever in the list when the selected service actually defines intake fields. Every service
    // that exists today defines none, so for them the list is what it always was.
    Intake,
    Day,
    Time,
    Review,
}
