using System;

namespace QueueApp.Shared.Templates.QueueEntry.Validators;

public static class ValidationLayoutExtensions
{
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
