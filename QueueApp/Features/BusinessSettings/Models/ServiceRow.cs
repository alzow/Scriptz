using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Features.BusinessSettings.Models;

// A service as the settings list shows it: the service itself, plus what is attached to it. The
// count comes from one read of the whole business's questions, not one read per row.
public sealed class ServiceRow : ObservableObject
{
    public required ServiceResponse Service { get; init; }
    public int QuestionCount { get; init; }

    public Guid Id => Service.Id;
    public string Name => Service.Name;
    public bool IsActive => Service.IsActive;

    public string DetailText => $"{Service.EstMinutes} min · {Service.PriceDisplay}";

    public bool HasQuestions => QuestionCount > 0;
    public string QuestionChipText => QuestionCount == 1 ? "1 QUESTION" : $"{QuestionCount} QUESTIONS";

    public bool RequiresCollection => Service.RequiresCollection;

    public static ServiceRow From(ServiceResponse service, int questionCount) =>
        new() { Service = service, QuestionCount = questionCount };
}
