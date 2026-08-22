using System.Collections;
using System.Windows.Input;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class ServicePickerView : ContentView
{
    public static readonly BindableProperty ServicesProperty = BindableProperty.Create(
        nameof(Services), typeof(IEnumerable), typeof(ServicePickerView), default(IEnumerable));
    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(ICommand), typeof(ServicePickerView), default(ICommand));
    public static readonly BindableProperty IsSectionVisibleProperty = BindableProperty.Create(
        nameof(IsSectionVisible), typeof(bool), typeof(ServicePickerView), default(bool));
    public static readonly BindableProperty IsEmptyProperty = BindableProperty.Create(
        nameof(IsEmpty), typeof(bool), typeof(ServicePickerView), default(bool));
    public static readonly BindableProperty SelectedServiceProperty = BindableProperty.Create(
        nameof(SelectedService), typeof(ServiceResponse), typeof(ServicePickerView), default(ServiceResponse));

    public ServiceResponse? SelectedService
    {
        get => (ServiceResponse?)GetValue(SelectedServiceProperty);
        set => SetValue(SelectedServiceProperty, value);
    }

    public IEnumerable Services
    {
        get => (IEnumerable)GetValue(ServicesProperty);
        set => SetValue(ServicesProperty, value);
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

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public ServicePickerView()
    {
        InitializeComponent();
    }
}
