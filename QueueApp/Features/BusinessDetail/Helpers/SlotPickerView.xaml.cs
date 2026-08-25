using System.Collections;
using System.Windows.Input;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class SlotPickerView : ContentView
{
    public static readonly BindableProperty SlotsProperty = BindableProperty.Create(
        nameof(Slots), typeof(IEnumerable), typeof(SlotPickerView), default(IEnumerable));
    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(ICommand), typeof(SlotPickerView), default(ICommand));
    public static readonly BindableProperty IsSectionVisibleProperty = BindableProperty.Create(
        nameof(IsSectionVisible), typeof(bool), typeof(SlotPickerView), default(bool));
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
        nameof(IsLoading), typeof(bool), typeof(SlotPickerView), default(bool));
    public static readonly BindableProperty IsEmptyProperty = BindableProperty.Create(
        nameof(IsEmpty), typeof(bool), typeof(SlotPickerView), default(bool));
    public static readonly BindableProperty SelectedSlotProperty = BindableProperty.Create(
        nameof(SelectedSlot), typeof(SlotResponse), typeof(SlotPickerView), default(SlotResponse));

    public SlotResponse? SelectedSlot
    {
        get => (SlotResponse?)GetValue(SelectedSlotProperty);
        set => SetValue(SelectedSlotProperty, value);
    }

    public IEnumerable Slots
    {
        get => (IEnumerable)GetValue(SlotsProperty);
        set => SetValue(SlotsProperty, value);
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

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public SlotPickerView()
    {
        InitializeComponent();
    }
}
