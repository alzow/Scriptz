using QueueApp.Features.Settings.Models;

namespace QueueApp.Features.Settings;

public partial class StaffManagementPage : ContentPage
{
    public StaffManagementPage()
    {
        InitializeComponent();
    }

    private void OnStaffRowDragStarting(object? sender, DragStartingEventArgs e)
    {
        if ((sender as Element)?.BindingContext is StaffRow row)
            e.Data.Properties["StaffRow"] = row;
    }

    private async void OnStaffRowDrop(object? sender, DropEventArgs e)
    {
        if (BindingContext is not StaffManagementPageViewModel viewModel)
            return;

        if (!e.Data.Properties.TryGetValue("StaffRow", out var value) || value is not StaffRow movedRow)
            return;

        if ((sender as Element)?.BindingContext is not StaffRow targetRow)
            return;

        await viewModel.ReorderAsync(movedRow, viewModel.ActiveStaff.IndexOf(targetRow));
    }
}
