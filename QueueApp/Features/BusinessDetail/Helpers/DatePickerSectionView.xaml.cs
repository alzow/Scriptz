using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class DatePickerSectionView : ContentView
{
    public static readonly BindableProperty DateOptionsProperty = BindableProperty.Create(
        nameof(DateOptions), typeof(IEnumerable), typeof(DatePickerSectionView), default(IEnumerable));
    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(ICommand), typeof(DatePickerSectionView), default(ICommand));
    public static readonly BindableProperty IsSectionVisibleProperty = BindableProperty.Create(
        nameof(IsSectionVisible), typeof(bool), typeof(DatePickerSectionView), default(bool));

    public IEnumerable DateOptions
    {
        get => (IEnumerable)GetValue(DateOptionsProperty);
        set => SetValue(DateOptionsProperty, value);
    }

    public ICommand SelectCommand
    {
        get => (ICommand)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public bool IsSectionVisible
    {
        get => (bool)GetValue(IsSectionVisibleProperty);
        set => SetValue(IsSectionVisibleProperty, value);
    }

    public DatePickerSectionView()
    {
        InitializeComponent();
    }
}
