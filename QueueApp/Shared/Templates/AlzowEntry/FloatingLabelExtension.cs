using System;
using System.Threading.Tasks;

namespace QueueApp.Shared.Templates.AlzowEntry;

public static class FloatingLabelExtensions
{
    private const uint AnimationDuration = 100;
    private const uint FontAnimationDuration = 250;
    private const double LabelUpTranslationY = -12;
    private const double ControlDownTranslationY = 8;
    private const double ResetTranslationY = 0;
    private const uint FontAnimationRate = 16;
    private const string FontSizeAnimationName = "FontSizeAnimation";

    private static async Task RunAnimationAsync(params Task[] animations)
    {
        await Task.Run(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.WhenAll(animations);
            });
        });
    }

    private static async Task RunSingleAnimationAsync(Task animation)
    {
        await Task.Run(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await animation;
            });
        });
    }

    public static async Task OnFocusAnimationAsync(this Label placeholderLabel, ImageButton rightIcon, Entry textEntry, bool showIconOnFocus)
    {
        if (!string.IsNullOrEmpty(placeholderLabel.Text))
        {
            rightIcon.IsVisible = showIconOnFocus;
            await RunAnimationAsync(
                placeholderLabel.TranslateTo(0, LabelUpTranslationY, AnimationDuration, Easing.Linear),
                textEntry.TranslateTo(0, ControlDownTranslationY, AnimationDuration, Easing.Linear)
            );
        }
        else
        {
            await RunSingleAnimationAsync(textEntry.TranslateTo(0, ResetTranslationY, AnimationDuration, Easing.Linear));
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                textEntry.VerticalOptions = LayoutOptions.Center;
                placeholderLabel.IsVisible = false;
            });
        }
    }

    public static void OnFocusAnimation(this Label placeholderLabel, ImageButton rightIcon, Entry textEntry, bool showIconOnFocus)
    {
        _ = OnFocusAnimationAsync(placeholderLabel, rightIcon, textEntry, showIconOnFocus);
    }

    public static async Task OnUnFocusAnimationAsync(this Label placeholderLabel, ImageButton rightIcon, Entry textEntry)
    {
        if (string.IsNullOrEmpty(textEntry.Text))
        {
            rightIcon.IsVisible = true;
            await RunAnimationAsync(
                placeholderLabel.TranslateTo(0, ResetTranslationY, AnimationDuration, Easing.Linear),
                textEntry.TranslateTo(0, ResetTranslationY, AnimationDuration, Easing.Linear)
            );
        }
    }

    public static void OnUnFocusAnimation(this Label placeholderLabel, ImageButton rightIcon, Entry textEntry)
    {
        _ = OnUnFocusAnimationAsync(placeholderLabel, rightIcon, textEntry);
    }

    public static async Task OnFocusAnimationAsync(this Label placeholderLabel, ImageButton rightIcon, DatePicker datePicker)
    {
        rightIcon.IsVisible = true;
        await RunAnimationAsync(
            placeholderLabel.TranslateTo(0, LabelUpTranslationY, AnimationDuration, Easing.Linear),
            datePicker.TranslateTo(0, ControlDownTranslationY, AnimationDuration, Easing.Linear)
        );
    }

    public static void OnFocusAnimation(this Label placeholderLabel, ImageButton rightIcon, DatePicker datePicker)
    {
        _ = OnFocusAnimationAsync(placeholderLabel, rightIcon, datePicker);
    }

    public static async Task OnUnFocusAnimationAsync(this Label placeholderLabel, ImageButton rightIcon, DatePicker datePicker)
    {
        if (string.IsNullOrEmpty(datePicker.Date.ToString()))
        {
            rightIcon.IsVisible = true;
            await RunAnimationAsync(
                placeholderLabel.TranslateTo(0, ResetTranslationY, AnimationDuration, Easing.Linear),
                datePicker.TranslateTo(0, ResetTranslationY, AnimationDuration, Easing.Linear)
            );
        }
    }

    public static void OnUnFocusAnimation(this Label placeholderLabel, ImageButton rightIcon, DatePicker datePicker)
    {
        _ = OnUnFocusAnimationAsync(placeholderLabel, rightIcon, datePicker);
    }

    public static async Task AnimateLabelFontSizeAsync(this Label label, double fontSize)
    {
        await Task.Run(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                label.Animate(
                    name: FontSizeAnimationName,
                    callback: x => label.FontSize = x,
                    start: label.FontSize,
                    end: fontSize,
                    rate: FontAnimationRate,
                    length: FontAnimationDuration,
                    easing: Easing.Linear
                );
            });
        });
    }

    public static void AnimateLabelFontSize(this Label label, double fontSize)
    {
        _ = AnimateLabelFontSizeAsync(label, fontSize);
    }
}
