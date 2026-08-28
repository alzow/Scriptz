using System.Windows.Input;

namespace QueueApp.Shared.Templates.TopBar;

public partial class TopBarView : ContentView
{
    public static readonly BindableProperty FlowTitleProperty = BindableProperty.Create(
        nameof(FlowTitle), typeof(string), typeof(TopBarView), string.Empty);

    public static readonly BindableProperty IsFlowActiveProperty = BindableProperty.Create(
        nameof(IsFlowActive), typeof(bool), typeof(TopBarView), false);

    public static readonly BindableProperty BusinessNameProperty = BindableProperty.Create(
        nameof(BusinessName), typeof(string), typeof(TopBarView), string.Empty);

    public static readonly BindableProperty GoBackCommandProperty = BindableProperty.Create(
        nameof(GoBackCommand), typeof(ICommand), typeof(TopBarView));

    public string FlowTitle
    {
        get => (string)GetValue(FlowTitleProperty);
        set => SetValue(FlowTitleProperty, value);
    }

    public bool IsFlowActive
    {
        get => (bool)GetValue(IsFlowActiveProperty);
        set => SetValue(IsFlowActiveProperty, value);
    }

    public string BusinessName
    {
        get => (string)GetValue(BusinessNameProperty);
        set => SetValue(BusinessNameProperty, value);
    }

    public ICommand? GoBackCommand
    {
        get => (ICommand?)GetValue(GoBackCommandProperty);
        set => SetValue(GoBackCommandProperty, value);
    }

    public TopBarView()
    {
        InitializeComponent();
    }
}
