namespace QueueApp.Features.IntakeAnswers.Constants;

public static class IntakeAnswersConstants
{
    public const string PageTitle = "Intake answers";
    public const string SectionTitle = "What they were asked";
    public const string EmptyText = "This one was never asked anything.";

    // The launcher needs a type to hand the OS. Storage keeps the uploaded content type on the
    // answer, but a reference written before that was carried has none, and octet-stream is the
    // one value every platform will still open with a chooser.
    public const string DefaultContentType = "application/octet-stream";
}
