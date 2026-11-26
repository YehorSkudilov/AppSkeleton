namespace AppSkeleton;

/// <summary>
/// A progress bar, drawn: a rounded track and the part done over it in <see cref="ProgressColor"/> (the accent by
/// default), with the track a faint wash of the same colour. In place of MAUI's ProgressBar, whose WinUI control sizes its
/// fill from its own width after each layout: at a scaling like 250% that came out a pixel wider, then narrower, every
/// pass, until WinUI gave up with a layout cycle and the app crashed. Drawn, it has nothing to lay out. Animate it with
/// <see cref="ProgressTo"/> (or <see cref="SmoothProgress"/>).
/// </summary>
public class ProgressLine : GraphicsView, IDrawable
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(ProgressLine), 0.0,
            coerceValue: (_, value) => Math.Clamp((double)value, 0, 1),
            propertyChanged: (bindable, _, _) => ((ProgressLine)bindable).Invalidate());

    public static readonly BindableProperty ProgressColorProperty =
        BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(ProgressLine), null,
            propertyChanged: (bindable, _, _) => ((ProgressLine)bindable).Invalidate());

    /// <summary>How much is done, 0 to 1.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>The part done's colour; null for the accent.</summary>
    public Color? ProgressColor
    {
        get => (Color?)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public ProgressLine()
    {
        Drawable = this;
        BackgroundColor = Colors.Transparent;
        HeightRequest = 4;
        InputTransparent = true;
    }

    /// <summary>Moves <see cref="Progress"/> to <paramref name="value"/> over <paramref name="length"/> ms, like ProgressBar.ProgressTo.</summary>
    public Task<bool> ProgressTo(double value, uint length, Easing easing)
    {
        var done = new TaskCompletionSource<bool>();
        this.AbortAnimation(nameof(ProgressTo));
        new Animation(v => Progress = v, Progress, Math.Clamp(value, 0, 1), easing)
            .Commit(this, nameof(ProgressTo), length: length, finished: (_, cancelled) => done.TrySetResult(cancelled));
        return done.Task;
    }

    void IDrawable.Draw(ICanvas canvas, RectF rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;
        var color = ProgressColor ?? (Application.Current?.Resources.TryGetValue("Accent", out var accent) == true && accent is Color c ? c : Colors.DodgerBlue);
        var radius = rect.Height / 2;
        canvas.FillColor = color.WithAlpha(0.18f);
        canvas.FillRoundedRectangle(rect, radius);
        var width = (float)(rect.Width * Progress);
        if (width <= 0)
            return;
        canvas.FillColor = color;
        // Never narrower than it is tall, so a little progress still shows as a rounded dot.
        canvas.FillRoundedRectangle(rect.X, rect.Y, Math.Max(width, rect.Height), rect.Height, radius);
    }
}
