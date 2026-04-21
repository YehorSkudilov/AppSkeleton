using Microsoft.Maui.ApplicationModel;

namespace AppSkeleton;

public partial class CChart : ContentView
{
    private IDrawable? _drawable;

    public static readonly BindableProperty ChartProperty =
        BindableProperty.Create(
            nameof(Chart),
            typeof(IDrawable),
            typeof(CChart),
            default(IDrawable),
            propertyChanged: OnDrawableChanged);

    public IDrawable Chart
    {
        get => (IDrawable)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    public CChart()
    {
        InitializeComponent();
    }

    private void SetChart(IDrawable drawable)
    {
        if (_drawable is CLineChart oldLine)
            oldLine.RedrawRequested -= OnRedrawRequested;

        if (_drawable is CPieChart oldPie)
            oldPie.RedrawRequested -= OnRedrawRequested;

        if (_drawable is CSegmentedBarChart oldBar)
            oldBar.RedrawRequested -= OnRedrawRequested;

        _drawable = drawable;
        ChartView.Drawable = drawable;

        if (drawable is CLineChart line)
            line.RedrawRequested += OnRedrawRequested;

        if (drawable is CPieChart pie)
            pie.RedrawRequested += OnRedrawRequested;

        if (drawable is CSegmentedBarChart bar)
            bar.RedrawRequested += OnRedrawRequested;

        ChartView.Invalidate(); // 🔥 ALWAYS FORCE
    }

    private void OnRedrawRequested()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ChartView.Invalidate();
        });
    }

    private static void OnDrawableChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CChart)bindable;

        control.SetChart(newValue as IDrawable);
        control.ChartView.Invalidate(); // 🔥 CRITICAL
    }
}