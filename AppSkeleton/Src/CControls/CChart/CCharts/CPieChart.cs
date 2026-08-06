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
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Entries == null || Entries.Count == 0)
            return;

        float total = Entries.Sum(v => v.Value);
        if (total <= 0)
            return;

        // 🔥 no padding at all
        float centerX = dirtyRect.Width / 2;
        float centerY = dirtyRect.Height / 2;

        float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.45f;

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

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            float sweep = (entry.Value / total) * 360f;

            canvas.FillColor = entry.Color ?? fallbackColors[i % fallbackColors.Length];

            float endAngle = startAngle + sweep;

            // slice
            var path = new PathF();
            path.MoveTo(centerX, centerY);
            AddArcToPath(path, centerX, centerY, radius, startAngle, endAngle);
            path.Close();

            canvas.FillPath(path);

            // -------------------------
            // LABEL (centered safely inside)
            // -------------------------
            float mid = startAngle + sweep / 2f;
            float rad = mid * (float)Math.PI / 180f;

            float labelRadius = radius * 0.6f;

            float x = centerX + labelRadius * (float)Math.Cos(rad);
            float y = centerY + labelRadius * (float)Math.Sin(rad);

            canvas.FontColor = Colors.White;
            canvas.FontSize = 12;

            if (!string.IsNullOrEmpty(LabelFontFamily))
                canvas.Font = new Microsoft.Maui.Graphics.Font(LabelFontFamily);

            canvas.DrawString(
                $"{entry.Label}\n{entry.Value:0}",
                x,
                y,
                HorizontalAlignment.Center);

            startAngle += sweep;
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