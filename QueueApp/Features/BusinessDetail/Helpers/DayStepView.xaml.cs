using System.Collections;
using System.Windows.Input;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class DayStepView : ContentView
{
    public static readonly BindableProperty ShowDayStepProperty = BindableProperty.Create(
        nameof(ShowDayStep), typeof(bool), typeof(DayStepView), false);

    public static readonly BindableProperty DayChoicesProperty = BindableProperty.Create(
        nameof(DayChoices), typeof(IEnumerable), typeof(DayStepView));

    public static readonly BindableProperty IsLoadingDaysProperty = BindableProperty.Create(
        nameof(IsLoadingDays), typeof(bool), typeof(DayStepView), false);

    public static readonly BindableProperty SelectedDayProperty = BindableProperty.Create(
        nameof(SelectedDay), typeof(DayChoiceItem), typeof(DayStepView));

    public static readonly BindableProperty ReviewOperatorLabelProperty = BindableProperty.Create(
        nameof(ReviewOperatorLabel), typeof(string), typeof(DayStepView), string.Empty);

    public static readonly BindableProperty SelectedOperatorChoiceProperty = BindableProperty.Create(
        nameof(SelectedOperatorChoice), typeof(OperatorChoiceItem), typeof(DayStepView));

    public static readonly BindableProperty DayFineprintProperty = BindableProperty.Create(
        nameof(DayFineprint), typeof(string), typeof(DayStepView), string.Empty);

    public static readonly BindableProperty SelectDayCommandProperty = BindableProperty.Create(
        nameof(SelectDayCommand), typeof(ICommand), typeof(DayStepView));

    public bool ShowDayStep
    {
        get => (bool)GetValue(ShowDayStepProperty);
        set => SetValue(ShowDayStepProperty, value);
    }

    public IEnumerable? DayChoices
    {
        get => (IEnumerable?)GetValue(DayChoicesProperty);
        set => SetValue(DayChoicesProperty, value);
    }

    public bool IsLoadingDays
    {
        get => (bool)GetValue(IsLoadingDaysProperty);
        set => SetValue(IsLoadingDaysProperty, value);
    }

    public DayChoiceItem? SelectedDay
    {
        get => (DayChoiceItem?)GetValue(SelectedDayProperty);
        set => SetValue(SelectedDayProperty, value);
    }

    public string ReviewOperatorLabel
    {
        get => (string)GetValue(ReviewOperatorLabelProperty);
        set => SetValue(ReviewOperatorLabelProperty, value);
    }

    public OperatorChoiceItem? SelectedOperatorChoice
    {
        get => (OperatorChoiceItem?)GetValue(SelectedOperatorChoiceProperty);
        set => SetValue(SelectedOperatorChoiceProperty, value);
    }

    public string DayFineprint
    {
        get => (string)GetValue(DayFineprintProperty);
        set => SetValue(DayFineprintProperty, value);
    }

    public ICommand? SelectDayCommand
    {
        get => (ICommand?)GetValue(SelectDayCommandProperty);
        set => SetValue(SelectDayCommandProperty, value);
    }

    public DayStepView()
    {
        InitializeComponent();
    }
}
