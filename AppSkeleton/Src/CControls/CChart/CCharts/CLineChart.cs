using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AppSkeleton;

public class CLineChart : BindableObject, IDrawable
{
    public event Action? RedrawRequested;

    // -------------------------
    // Entries
    // -------------------------
    public static readonly BindableProperty EntriesProperty =
        BindableProperty.Create(
            nameof(Entries),
            typeof(ObservableCollection<CChartEntry>),
            typeof(CLineChart),
            new ObservableCollection<CChartEntry>(),
            propertyChanged: OnEntriesChanged);

    public ObservableCollection<CChartEntry> Entries
    {
        get => (ObservableCollection<CChartEntry>)GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    private static void OnEntriesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var chart = (CLineChart)bindable;

        if (oldValue is ObservableCollection<CChartEntry> oldCol)
            oldCol.CollectionChanged -= chart.OnCollectionChanged;

        if (newValue is ObservableCollection<CChartEntry> newCol)
            newCol.CollectionChanged += chart.OnCollectionChanged;

        chart.RequestRedraw();
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RequestRedraw();
    }

    // -------------------------
    // SMOOTH
    // -------------------------
    public static readonly BindableProperty SmoothProperty =
        BindableProperty.Create(
            nameof(Smooth),
            typeof(bool),
            typeof(CLineChart),
            false,
            propertyChanged: (b, o, n) => ((CLineChart)b).RequestRedraw());

    public bool Smooth
    {
        get => (bool)GetValue(SmoothProperty);
        set => SetValue(SmoothProperty, value);
    }

    private void RequestRedraw()
    {
        RedrawRequested?.Invoke();
    }

    // -------------------------
    // DRAW
    // -------------------------
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Entries == null || Entries.Count < 2)
            return;

        float paddingTop = 20;
        float paddingBottom = 20;
        float paddingLeft = 20;
        float paddingRight = 20;

        float width = dirtyRect.Width - paddingLeft - paddingRight;
        float height = dirtyRect.Height - paddingTop - paddingBottom;

        float max = Entries.Max(v => v.Value);
        float min = Entries.Min(v => v.Value);

        float range = max - min;
        if (range == 0) range = 1;

        float yPadding = range * 0.1f;
        max += yPadding;
        min -= yPadding;
        range = max - min;

        float stepX = width / (Entries.Count - 1);

        var points = new List<PointF>();

        for (int i = 0; i < Entries.Count; i++)
        {
            float x = paddingLeft + i * stepX;
            float y = paddingTop + (height - ((Entries[i].Value - min) / range * height));

            points.Add(new PointF(x, y));
        }

        var path = new PathF();

        if (!Smooth)
        {
            path.MoveTo(points[0].X, points[0].Y);

            for (int i = 1; i < points.Count; i++)
                path.LineTo(points[i].X, points[i].Y);
        }
        else
        {
            path.MoveTo(points[0].X, points[0].Y);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = i > 0 ? points[i - 1] : points[i];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = i + 2 < points.Count ? points[i + 2] : p2;

                float cp1x = p1.X + (p2.X - p0.X) / 6f;
                float cp1y = p1.Y + (p2.Y - p0.Y) / 6f;

                float cp2x = p2.X - (p3.X - p1.X) / 6f;
                float cp2y = p2.Y - (p3.Y - p1.Y) / 6f;

                path.CurveTo(cp1x, cp1y, cp2x, cp2y, p2.X, p2.Y);
            }
        }

        // LINE
        canvas.StrokeColor = Colors.DodgerBlue;
        canvas.StrokeSize = 3;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeLineCap = LineCap.Round;

        canvas.DrawPath(path);

        // -------------------------
        // AXIS LINE (draw ONCE)
        // -------------------------
        float axisY = paddingTop + height;

        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 1;

        canvas.DrawLine(
            paddingLeft,
            axisY,
            paddingLeft + width,
            axisY);

        // -------------------------
        // POINTS + LABELS
        // -------------------------
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var entry = Entries[i];

            // POINT
            canvas.FillColor = entry.Color ?? Colors.DodgerBlue;
            canvas.FillCircle(p.X, p.Y, 3);

            canvas.FontSize = 12;

            // -------------------------
            // VALUE LABEL (above/below point)
            // -------------------------
            float valueY = p.Y - 14;

            if (valueY < paddingTop)
                valueY = p.Y + 14;

            valueY = Math.Clamp(valueY, 10, dirtyRect.Height - 10);

            canvas.FontColor = Colors.White;

            canvas.DrawString(
                entry.Value.ToString("0"),
                p.X,
                valueY,
                HorizontalAlignment.Center);

            // -------------------------
            // X LABEL (below axis)
            // -------------------------
            float labelX = Math.Clamp(p.X, paddingLeft + 10, paddingLeft + width - 10);
            float labelY = axisY + 15;

            canvas.FontColor = Colors.Gray;

            canvas.DrawString(
                entry.Label,
                labelX,
                labelY,
                HorizontalAlignment.Center);
        }
    }
}