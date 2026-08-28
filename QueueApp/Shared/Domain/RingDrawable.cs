using Microsoft.Maui.Graphics;

namespace QueueApp.Shared.Domain;

// The draining ring on the queue ticket. A new instance is assigned whenever progress moves, which
// is what makes the GraphicsView repaint — the view model has no handle on the view to invalidate.
public sealed class RingDrawable : IDrawable
{
    private const float StrokeWidth = 6f;

    private readonly float _progress;

    public RingDrawable(double progress)
    {
        _progress = (float)Math.Clamp(progress, 0d, 1d);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (size <= StrokeWidth * 2)
            return;

        var radius = (size - StrokeWidth) / 2f;
        var centre = new PointF(dirtyRect.Center.X, dirtyRect.Center.Y);
        var box = new RectF(centre.X - radius, centre.Y - radius, radius * 2, radius * 2);

        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;

        canvas.StrokeColor = Color.FromArgb("#252C39");
        canvas.DrawEllipse(box);

        if (_progress <= 0f)
            return;

        canvas.StrokeColor = Color.FromArgb("#39FF7A");
        canvas.DrawArc(box, 90f, 90f - (360f * _progress), true, false);
    }
}
