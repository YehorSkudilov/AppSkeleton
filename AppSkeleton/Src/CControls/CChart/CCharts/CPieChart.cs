using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AppSkeleton;

public class CPieChart : BindableObject, IDrawable
{
    public event Action? RedrawRequested;

    // -------------------------
    // Entries
    // -------------------------
    public static readonly BindableProperty EntriesProperty =
        BindableProperty.Create(
            nameof(Entries),
            typeof(ObservableCollection<CChartEntry>),
            typeof(CPieChart),
            new ObservableCollection<CChartEntry>(),
            propertyChanged: OnEntriesChanged);

    public ObservableCollection<CChartEntry> Entries
    {
        get => (ObservableCollection<CChartEntry>)GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    // -------------------------
    // Label font family (e.g. an icon font alias, so Label can hold a glyph name)
    // -------------------------
    public static readonly BindableProperty LabelFontFamilyProperty =
        BindableProperty.Create(
            nameof(LabelFontFamily),
            typeof(string),
            typeof(CPieChart),
            default(string));

    public string LabelFontFamily
    {
        get => (string)GetValue(LabelFontFamilyProperty);
        set => SetValue(LabelFontFamilyProperty, value);
    }

    private static void OnEntriesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var chart = (CPieChart)bindable;

        if (oldValue is ObservableCollection<CChartEntry> oldCol)
            oldCol.CollectionChanged -= chart.OnCollectionChanged;

        if (newValue is ObservableCollection<CChartEntry> newCol)
            newCol.CollectionChanged += chart.OnCollectionChanged;

        chart.RequestRedraw();
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        => RequestRedraw();

    private void RequestRedraw()
        => RedrawRequested?.Invoke();

    // -------------------------
    // DRAW
    // -------------------------
    private const float LabelColumnWidth = 92f;
    private const float LabelRowHeight = 22f;
    private const float LeaderStubLength = 16f;
    private const float LeaderTextGap = 6f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Entries == null || Entries.Count == 0)
            return;

        float total = Entries.Sum(v => v.Value);
        if (total <= 0)
            return;

        float centerX = dirtyRect.Width / 2;
        float centerY = dirtyRect.Height / 2;

        // The circle has to leave room on both sides for the label columns and their
        // leader lines, so it's sized off the narrower of "width minus both columns" and
        // "height", not just the raw dirtyRect like an inline-labeled pie would be.
        float maxRadiusByWidth = centerX - LabelColumnWidth - LeaderStubLength;
        float maxRadiusByHeight = centerY - 4f;
        float radius = Math.Max(10f, Math.Min(maxRadiusByWidth, maxRadiusByHeight));

        float startAngle = -90f;

        var fallbackColors = new[]
        {
            Colors.DodgerBlue,
            Colors.Orange,
            Colors.MediumSeaGreen,
            Colors.MediumPurple,
            Colors.IndianRed,
            Colors.Gold,
            Colors.CornflowerBlue
        };

        var leftSlices = new List<(CChartEntry Entry, float Mid, Color Color)>();
        var rightSlices = new List<(CChartEntry Entry, float Mid, Color Color)>();

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            float sweep = (entry.Value / total) * 360f;
            var color = entry.Color ?? fallbackColors[i % fallbackColors.Length];

            canvas.FillColor = color;

            float endAngle = startAngle + sweep;

            var path = new PathF();
            path.MoveTo(centerX, centerY);
            AddArcToPath(path, centerX, centerY, radius, startAngle, endAngle);
            path.Close();

            canvas.FillPath(path);

            float mid = startAngle + sweep / 2f;
            float rad = mid * (float)Math.PI / 180f;

            // Split into left/right halves by which side of the circle the slice's
            // midpoint actually sits on, so labels only ever land beside the pie - never
            // above or below it.
            if (Math.Cos(rad) < 0)
                leftSlices.Add((entry, mid, color));
            else
                rightSlices.Add((entry, mid, color));

            startAngle += sweep;
        }

        if (!string.IsNullOrEmpty(LabelFontFamily))
            canvas.Font = new Microsoft.Maui.Graphics.Font(LabelFontFamily);

        DrawLabelColumn(canvas, leftSlices, isLeft: true, centerX, centerY, radius, total, dirtyRect);
        DrawLabelColumn(canvas, rightSlices, isLeft: false, centerX, centerY, radius, total, dirtyRect);
    }

    private void DrawLabelColumn(ICanvas canvas, List<(CChartEntry Entry, float Mid, Color Color)> slices, bool isLeft, float centerX, float centerY, float radius, float total, RectF dirtyRect)
    {
        if (slices.Count == 0)
            return;

        // Order top-to-bottom by the slice's actual edge height, not by raw angle - on the
        // left half of the circle, sin(angle) is decreasing as angle increases, so sorting
        // by angle instead would put labels in the opposite order from their slices and
        // send every leader line crossing through its neighbors.
        slices.Sort((a, b) =>
        {
            float ay = (float)Math.Sin(a.Mid * (float)Math.PI / 180f);
            float by = (float)Math.Sin(b.Mid * (float)Math.PI / 180f);
            return ay.CompareTo(by);
        });

        // Desired row = the height at which the slice's own edge point sits, so a label
        // naturally lines up with its slice when there's no crowding to resolve.
        float[] y = new float[slices.Count];
        for (int i = 0; i < slices.Count; i++)
        {
            float rad = slices[i].Mid * (float)Math.PI / 180f;
            y[i] = centerY + radius * (float)Math.Sin(rad);
        }

        float topBound = dirtyRect.Top + LabelRowHeight / 2f;
        float bottomBound = dirtyRect.Bottom - LabelRowHeight / 2f;

        // Two-pass separation (push down, then push up) plus a final clamp - the standard
        // approach for spacing out sorted labels along an axis without letting them
        // overlap, even when several slices' midpoints land close together.
        for (int i = 1; i < y.Length; i++)
            y[i] = Math.Max(y[i], y[i - 1] + LabelRowHeight);

        if (y[^1] > bottomBound)
        {
            y[^1] = bottomBound;
            for (int i = y.Length - 2; i >= 0; i--)
                y[i] = Math.Min(y[i], y[i + 1] - LabelRowHeight);
        }

        if (y[0] < topBound)
        {
            y[0] = topBound;
            for (int i = 1; i < y.Length; i++)
                y[i] = Math.Max(y[i], y[i - 1] + LabelRowHeight);
        }

        float elbowX = isLeft ? centerX - radius - LeaderStubLength : centerX + radius + LeaderStubLength;

        for (int i = 0; i < slices.Count; i++)
        {
            var (entry, mid, color) = slices[i];
            float rad = mid * (float)Math.PI / 180f;

            float edgeX = centerX + radius * (float)Math.Cos(rad);
            float edgeY = centerY + radius * (float)Math.Sin(rad);

            // Leader line: out horizontally from the slice edge to the label column, then
            // up/down to the resolved row - never cuts back across the pie because the
            // horizontal leg travels away from center at the edge point's own height.
            canvas.StrokeColor = color.WithAlpha(0.7f);
            canvas.StrokeSize = 1.25f;
            canvas.DrawLine(edgeX, edgeY, elbowX, edgeY);
            if (Math.Abs(edgeY - y[i]) > 0.5f)
                canvas.DrawLine(elbowX, edgeY, elbowX, y[i]);

            canvas.FillColor = color;
            canvas.FillCircle(elbowX, y[i], 2f);

            canvas.FontColor = Colors.White;
            canvas.FontSize = 13;

            float percent = entry.Value / total * 100f;
            string text = $"{entry.Label} {percent:0}%";

            var textRect = isLeft
                ? new RectF(dirtyRect.Left, y[i] - LabelRowHeight / 2f, elbowX - LeaderTextGap - dirtyRect.Left, LabelRowHeight)
                : new RectF(elbowX + LeaderTextGap, y[i] - LabelRowHeight / 2f, dirtyRect.Right - (elbowX + LeaderTextGap), LabelRowHeight);

            canvas.DrawString(text, textRect, isLeft ? HorizontalAlignment.Right : HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }

    // -------------------------
    // ARC (fixed smoothness)
    // -------------------------
    private void AddArcToPath(PathF path, float cx, float cy, float r, float startAngle, float endAngle)
    {
        int segments = 30;
        float step = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + i * step) * (float)Math.PI / 180f;

            float x = cx + r * (float)Math.Cos(angle);
            float y = cy + r * (float)Math.Sin(angle);

            path.LineTo(x, y);
        }
    }
}