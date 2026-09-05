using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Features.BusinessSettings.Models;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Features.BusinessSettings.Helpers;

// One service's questions: the rows the settings page lists, what they cost the customer, and the
// ordering rule that a conditional question depends on. Shows nothing and navigates nowhere — a
// refused move comes back as a reason for the view model to put in front of the owner.
public sealed class ServiceQuestionsEditor : ObservableObject
{
    public ObservableCollection<IntakeQuestionRow> Rows { get; } = new();

    public bool HasQuestions => Rows.Count > 0;
    public bool IsEmpty => Rows.Count == 0;

    public string SummaryLine => IntakeQuestionHelper.SummaryLine(_fields);
    public string CostLine => IntakeQuestionHelper.CostLine(_fields.Count);

    private List<IntakeFieldResponse> _fields = new();

    private readonly IIntakeFieldsService _intakeFieldsService;

    public ServiceQuestionsEditor(IIntakeFieldsService intakeFieldsService)
    {
        _intakeFieldsService = intakeFieldsService;
    }

    public IReadOnlyList<IntakeFieldResponse> Fields => _fields;

    public async Task LoadAsync(Guid serviceId)
    {
        var fields = await _intakeFieldsService.GetFieldsForServiceAsync(serviceId);
        _fields = fields.OrderBy(f => f.SortOrder).ToList();
        Rebuild();
    }

    public void Clear()
    {
        _fields = new List<IntakeFieldResponse>();
        Rebuild();
    }

    // Null when the move happened. Otherwise the reason it didn't, naming both questions — a drag
    // that silently springs back teaches the owner nothing.
    public async Task<string?> MoveAsync(IntakeQuestionRow? row, int direction)
    {
        if (row is null)
            return null;

        var index = _fields.FindIndex(f => f.Id == row.Id);
        var target = index + direction;

        if (index < 0 || target < 0 || target >= _fields.Count)
            return null;

        var refusal = IntakeQuestionHelper.DescribeBrokenOrder(_fields, index, target);
        if (refusal is not null)
            return refusal;

        var field = _fields[index];
        _fields.RemoveAt(index);
        _fields.Insert(target, field);

        // Rows written before sort_order was maintained can share a value, and swapping two equal
        // numbers is a no-op. Renumbering the whole list from its own order is what makes the move
        // survive the next read.
        var writes = new List<Task>(_fields.Count);
        for (var position = 0; position < _fields.Count; position++)
        {
            _fields[position].SortOrder = position;
            writes.Add(_intakeFieldsService.SetFieldOrderAsync(_fields[position].Id, position));
        }

        await Task.WhenAll(writes);

        Rebuild();
        return null;
    }

    public void Rebuild()
    {
        Rows.Clear();
        foreach (var field in _fields)
            Rows.Add(IntakeQuestionRow.From(field, _fields));

        OnPropertyChanged(nameof(HasQuestions));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(SummaryLine));
        OnPropertyChanged(nameof(CostLine));
    }
}
