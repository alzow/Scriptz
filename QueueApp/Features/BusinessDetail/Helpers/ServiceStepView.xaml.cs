using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class ServiceStepView : ContentView
{
    public static readonly BindableProperty ServiceRowsProperty = BindableProperty.Create(
        nameof(ServiceRows), typeof(IEnumerable), typeof(ServiceStepView));

    public static readonly BindableProperty ShowServiceStepProperty = BindableProperty.Create(
        nameof(ShowServiceStep), typeof(bool), typeof(ServiceStepView), false);

    public static readonly BindableProperty SelectServiceCommandProperty = BindableProperty.Create(
        nameof(SelectServiceCommand), typeof(ICommand), typeof(ServiceStepView));

    public IEnumerable? ServiceRows
    {
        get => (IEnumerable?)GetValue(ServiceRowsProperty);
        set => SetValue(ServiceRowsProperty, value);
    }

    public bool ShowServiceStep
    {
        get => (bool)GetValue(ShowServiceStepProperty);
        set => SetValue(ShowServiceStepProperty, value);
    }

    public ICommand? SelectServiceCommand
    {
        get => (ICommand?)GetValue(SelectServiceCommandProperty);
        set => SetValue(SelectServiceCommandProperty, value);
    }

    public ServiceStepView()
    {
        InitializeComponent();
    }
}
