using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AppSkeleton;

public class CBarChart : BindableObject, IDrawable
{
    public event Action? RedrawRequested;

    // -------------------------
    // ENTRIES
    // -------------------------
    public static readonly BindableProperty EntriesProperty =
        BindableProperty.Create(
            nameof(Entries),
            typeof(ObservableCollection<CChartEntry>),
            typeof(CBarChart),
            new ObservableCollection<CChartEntry>(),
            propertyChanged: OnEntriesChanged);

    public ObservableCollection<CChartEntry> Entries
    {
        get => (ObservableCollection<CChartEntry>)GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    private static void OnEntriesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var chart = (CBarChart)bindable;

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
    // ORIENTATION
    // -------------------------
    public static readonly BindableProperty IsHorizontalProperty =
        BindableProperty.Create(
            nameof(IsHorizontal),
            typeof(bool),
            typeof(CBarChart),
            false,
            propertyChanged: (b, o, n) => ((CBarChart)b).RequestRedraw());

    public bool IsHorizontal
    {
        get => (bool)GetValue(IsHorizontalProperty);
        set => SetValue(IsHorizontalProperty, value);
    }

    private void RequestRedraw() => RedrawRequested?.Invoke();

    // -------------------------
    // DRAW
    // -------------------------
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Entries == null || Entries.Count == 0)
            return;

        // -------------------------
        // SPACING CONFIG
        // -------------------------
        float padding = 20;

        float labelGap = 22;        // space between bar and label
        float bottomSpace = 5;     // bottom label zone
        float sideSpace = 80;       // left label zone (horizontal)

        float width = dirtyRect.Width - padding * 2 - (IsHorizontal ? sideSpace : 0);
        float height = dirtyRect.Height - padding * 2 - (!IsHorizontal ? bottomSpace : 0);

        // Only used by the horizontal layout below - negative entries there still
        // render as a flat, zero-width bar rather than one that extends past the left
        // edge. The vertical layout handles negatives properly with its own zero-line
        // split instead (see maxPositive/maxNegative below).
        float max = Entries.Max(e => Math.Max(0, e.Value));
        if (max == 0) max = 1;

        int count = Entries.Count;
        float spacing = 10;

        canvas.FontSize = 12;

        // =====================================================
        // VERTICAL BAR CHART
        // =====================================================
        if (!IsHorizontal)
        {
            float barWidth = (width - (count - 1) * spacing) / count;

            // Split the available height into a zone above the zero line (for
            // positive entries) and a zone below it (for negative ones), sized
            // proportionally to the largest magnitude on each side - not a fixed
            // 50/50 split, so an all-positive (or all-negative) set of entries still
            // uses the full height exactly like before negative values were handled
            // at all.
            float maxPositive = Entries.Max(e => Math.Max(0, e.Value));
            float maxNegative = Entries.Max(e => Math.Max(0, -e.Value));
            float totalRange = maxPositive + maxNegative;

            float positiveZoneHeight = totalRange > 0 ? height * (maxPositive / totalRange) : height;
            float negativeZoneHeight = height - positiveZoneHeight;
            float baselineY = padding + positiveZoneHeight;

            // Floored to 1 only for the division below - positiveZoneHeight/
            // negativeZoneHeight are already correctly 0 on whichever side has no
            // entries, so this can't make a bar appear where it shouldn't.
            float positiveScale = maxPositive == 0 ? 1 : maxPositive;
            float negativeScale = maxNegative == 0 ? 1 : maxNegative;

            // Zero line - only meaningful, and only drawn, when entries actually
            // split across both sides of it.
            if (positiveZoneHeight > 0 && negativeZoneHeight > 0)
            {
                canvas.StrokeColor = Colors.Gray;
                canvas.StrokeSize = 1;
                canvas.DrawLine(padding, baselineY, padding + width, baselineY);
            }

            for (int i = 0; i < count; i++)
            {
                var entry = Entries[i];

                float x = padding + i * (barWidth + spacing);
                float y, barHeight;

                if (entry.Value >= 0)
                {
                    barHeight = (entry.Value / positiveScale) * positiveZoneHeight;
                    y = baselineY - barHeight;
                }
                else
                {
                    barHeight = (-entry.Value / negativeScale) * negativeZoneHeight;
                    y = baselineY;
                }

                // BAR
                canvas.FillColor = entry.Color ?? Colors.DodgerBlue;
                canvas.FillRectangle(x, y, barWidth, barHeight);

                // VALUE LABEL - above a positive bar (or inside it if that would
                // clip past the top), below a negative one since it hangs downward.
                canvas.FontColor = Colors.White;

                float valueY;
                if (entry.Value >= 0)
                {
                    valueY = y - 12;
                    if (valueY < padding)
                        valueY = y + 15;
                }
                else
                {
                    valueY = y + barHeight + 15;
                }

                canvas.DrawString(
                    entry.Value.ToString("$#,0;-$#,0;$0"),
                    x + barWidth / 2,
                    valueY,
                    HorizontalAlignment.Center);

                // CATEGORY LABEL (always at the very bottom of the chart area,
                // regardless of which side of the zero line this bar landed on)
                canvas.FontColor = Colors.White;

                canvas.DrawString(
                    entry.Label,
                    x + barWidth / 2,
                    padding + height + labelGap,
                    HorizontalAlignment.Center);
            }
        }

        // =====================================================
        // HORIZONTAL BAR CHART
        // =====================================================
        else
        {
            float barHeight = (height - (count - 1) * spacing) / count;

            for (int i = 0; i < count; i++)
            {
                var entry = Entries[i];

                float barWidth = (Math.Max(0, entry.Value) / max) * width;

                float x = padding + sideSpace;
                float y = padding + i * (barHeight + spacing);

                // BAR
                canvas.FillColor = entry.Color ?? Colors.DodgerBlue;
                canvas.FillRectangle(x, y, barWidth, barHeight);

                // LABEL (left side with padding)
                canvas.FontColor = Colors.Gray;

                canvas.DrawString(
                    entry.Label,
                    padding + 10,
                    y + barHeight / 2,
                    HorizontalAlignment.Left);

                // VALUE (right side with clamp)
                canvas.FontColor = Colors.White;

                float valueX = x + barWidth + 6;
                float maxX = dirtyRect.Width - padding;

                if (valueX > maxX)
                    valueX = maxX - 5;

                canvas.DrawString(
                    entry.Value.ToString("$#,0;-$#,0;$0"),
                    valueX,
                    y + barHeight / 2,
                    HorizontalAlignment.Left);
            }
        }
    }
}