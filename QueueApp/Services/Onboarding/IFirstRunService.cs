namespace QueueApp.Services.Onboarding;

public interface IFirstRunService
{
    bool HasSeenWelcome { get; }

    void MarkWelcomeSeen();
}
