namespace QueueApp.Framework.Behaviors;

// TriggerAction used from a DataTrigger's EnterActions/ExitActions to animate Scale
// instead of the instant jump a plain Setter would cause.
public class ScaleTriggerAction : TriggerAction<VisualElement>
{
    public double Scale { get; set; } = 1.0;

    protected override void Invoke(VisualElement sender)
    {
        sender.ScaleTo(Scale, 150, Easing.CubicOut);
    }
}
