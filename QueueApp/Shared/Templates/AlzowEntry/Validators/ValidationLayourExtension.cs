using System;

namespace QueueApp.Shared.Templates.AlzowEntry.Validators;

public static class ValidationLayoutExtensions
{
    public static async Task<bool> IsValid(this IBindableLayout view)
    {
        foreach (var child in view.Children)
        {
            if (child is IValidationView validationView && (!validationView.Validate() || !await validationView.ValidateAsync()) && ((ContentView)child).IsVisible)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsActuallyVisible(this VisualElement element)
    {
        while (element != null)
        {
            if (!element.IsVisible)
                return false;

            element = element.Parent as VisualElement;
        }

        return true;
    }
}
