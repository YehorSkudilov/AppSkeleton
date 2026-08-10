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
            float labelRadius = radius * 0.6f;

            // A slice thinner (along its arc, at the radius the badge sits on) than the
            // badge itself has no way to fit it without overlapping whichever neighbor
            // it lands closest to - skip the badge/value entirely below that width
            // rather than drawing something guaranteed to collide. Computed from the
            // actual badge size and radius (not a flat percentage cutoff) so it holds at
            // any chart/canvas size instead of being tuned for one.
            const float badgeRadius = 13f;
            const float minBadgeGapFactor = 2.2f; // headroom so neighboring badges don't just barely touch
            float minSweepForLabel = (badgeRadius * minBadgeGapFactor) / labelRadius * (180f / (float)Math.PI);

            if (sweep >= minSweepForLabel)
            {
                float mid = startAngle + sweep / 2f;
                float rad = mid * (float)Math.PI / 180f;

                float x = centerX + labelRadius * (float)Math.Cos(rad);
                float y = centerY + labelRadius * (float)Math.Sin(rad);

                // Circle badge behind the emoji - same translucent-white icon-badge look
                // used elsewhere (e.g. BudgetListView's category icon), rather than the
                // glyph floating bare on the slice.
                float badgeCenterY = y - 8f;

                canvas.FillColor = Color.FromArgb("#30ffffff");
                canvas.FillCircle(x, badgeCenterY, badgeRadius);

                canvas.StrokeColor = Color.FromArgb("#40ffffff");
                canvas.StrokeSize = 1;
                canvas.DrawCircle(x, badgeCenterY, badgeRadius);

                canvas.FontColor = Colors.White;

                if (!string.IsNullOrEmpty(LabelFontFamily))
                    canvas.Font = new Microsoft.Maui.Graphics.Font(LabelFontFamily);

                // The point-based DrawString overload anchors by the text's top, not its
                // center, so the emoji was landing below the badge rather than inside it -
                // the rect-based overload with VerticalAlignment.Center actually centers on
                // both axes.
                canvas.FontSize = 14;
                var badgeRect = new RectF(x - badgeRadius, badgeCenterY - badgeRadius, badgeRadius * 2, badgeRadius * 2);
                canvas.DrawString(entry.Label, badgeRect, HorizontalAlignment.Center, VerticalAlignment.Center);

                // Value sits in its own rect below the badge, with a real gap instead of
                // being packed against it.
                const float valueGap = 3f;
                const float valueHeight = 16f;
                canvas.FontSize = 12;
                var valueRect = new RectF(x - 20, badgeCenterY + badgeRadius + valueGap, 40, valueHeight);
                canvas.DrawString($"{entry.Value / total * 100:0}%", valueRect, HorizontalAlignment.Center, VerticalAlignment.Center);
            }

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