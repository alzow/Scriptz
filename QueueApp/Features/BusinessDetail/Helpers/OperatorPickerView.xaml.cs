using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class OperatorPickerView : ContentView
{
    public static readonly BindableProperty OperatorsProperty = BindableProperty.Create(
        nameof(Operators), typeof(IEnumerable), typeof(OperatorPickerView), default(IEnumerable));
    public static readonly BindableProperty SelectedOperatorProperty = BindableProperty.Create(
        nameof(SelectedOperator), typeof(object), typeof(OperatorPickerView), default, BindingMode.TwoWay);
    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(ICommand), typeof(OperatorPickerView), default(ICommand));
    public static readonly BindableProperty IsSectionVisibleProperty = BindableProperty.Create(
        nameof(IsSectionVisible), typeof(bool), typeof(OperatorPickerView), default(bool));

    public IEnumerable Operators
    {
        get => (IEnumerable)GetValue(OperatorsProperty);
        set => SetValue(OperatorsProperty, value);
    }

    public object SelectedOperator
    {
        get => GetValue(SelectedOperatorProperty);
        set => SetValue(SelectedOperatorProperty, value);
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

    public OperatorPickerView()
    {
        InitializeComponent();
    }
}
