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

    // Keeps an "inside the bar" value label within the bar's own vertical extent - the
    // fixed +15/-12 offsets used to nudge a label in from an edge assume a bar tall
    // enough to absorb that offset. On a short bar (small-magnitude entry) they'd
    // otherwise push the label past the bar's far edge and toward the zero-line
    // splitter on the other side. Too short to offset at all - center on the bar instead.
    private static float ClampInsideBar(float desiredY, float barTop, float barHeight)
    {
        if (barHeight < 16)
            return barTop + barHeight / 2f;

        return Math.Clamp(desiredY, barTop + 4, barTop + barHeight - 4);
    }

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

            // A strict value-proportional split can starve the smaller side of a
            // lopsided pair (e.g. 400 vs -133 gives the negative zone only ~25% of the
            // height) down to just its bar with no room left for that bar's own value
            // label - no per-bar fallback can fix a zone that's too thin in the first
            // place. Guarantee each side that actually has entries at least half the
            // height (or the full target floor, whichever is less) - capping the floor
            // at height/2 instead of gating the whole thing behind "is the chart tall
            // enough for both full floors" means a compact chart still gets an even
            // 50/50 split rather than silently reverting to the unprotected
            // proportional split that recreates this exact bug.
            const float minZoneHeight = 50f;
            if (maxPositive > 0 && maxNegative > 0)
            {
                float minEachSide = Math.Min(minZoneHeight, height / 2f);
                if (positiveZoneHeight < minEachSide)
                {
                    positiveZoneHeight = minEachSide;
                    negativeZoneHeight = height - positiveZoneHeight;
                }
                else if (negativeZoneHeight < minEachSide)
                {
                    negativeZoneHeight = minEachSide;
                    positiveZoneHeight = height - negativeZoneHeight;
                }
            }

            float baselineY = padding + positiveZoneHeight;

            // Floored to 1 only for the division below - positiveZoneHeight/
            // negativeZoneHeight are already correctly 0 on whichever side has no
            // entries, so this can't make a bar appear where it shouldn't.
            float positiveScale = maxPositive == 0 ? 1 : maxPositive;
            float negativeScale = maxNegative == 0 ? 1 : maxNegative;

            // Zero line - only meaningful, and only drawn, when entries actually
            // split across both sides of it.
            bool hasZeroLine = positiveZoneHeight > 0 && negativeZoneHeight > 0;
            if (hasZeroLine)
            {
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = 1;
                canvas.DrawLine(padding, baselineY, padding + width, baselineY);
            }

            // A small gap so bars float off the zero line instead of touching/
            // overlapping it - only applied when the line is actually drawn, so an
            // all-positive (or all-negative) chart still uses its full zone height
            // right down to the edge like before this existed.
            const float zeroLineGap = 4f;
            float gap = hasZeroLine ? zeroLineGap : 0f;

            // A bar scaled strictly by value can end up just a few pixels tall next to
            // a much larger one on the same side, leaving no room for its own value
            // label to sit above/below it without crowding the zero line. Floor every
            // non-zero bar to at least this tall (capped by its own zone, so it still
            // can't spill into the other side) - the same "minimum slice" tradeoff pie
            // charts make for slivers, trading strict proportionality for a label that
            // actually fits.
            const float minBarHeight = 24f;

            for (int i = 0; i < count; i++)
            {
                var entry = Entries[i];

                float x = padding + i * (barWidth + spacing);
                float y, barHeight;

                if (entry.Value >= 0)
                {
                    float usableHeight = Math.Max(0, positiveZoneHeight - gap);
                    barHeight = (entry.Value / positiveScale) * usableHeight;
                    if (entry.Value > 0)
                        barHeight = Math.Min(usableHeight, Math.Max(barHeight, minBarHeight));
                    y = baselineY - gap - barHeight;
                }
                else
                {
                    float usableHeight = Math.Max(0, negativeZoneHeight - gap);
                    barHeight = (-entry.Value / negativeScale) * usableHeight;
                    if (entry.Value < 0)
                        barHeight = Math.Min(usableHeight, Math.Max(barHeight, minBarHeight));
                    y = baselineY + gap;
                }

                // BAR
                canvas.FillColor = entry.Color ?? Colors.DodgerBlue;
                canvas.FillRectangle(x, y, barWidth, barHeight);

                // VALUE LABEL - above a positive bar (or inside it if that would clip
                // past the top), below a negative one (or inside it, near the bottom,
                // if that would otherwise overlap the category label row beneath the
                // chart - same "not enough room outside, so go inside" fallback the
                // positive case already uses at the opposite edge).
                canvas.FontColor = Colors.White;

                float valueY;
                if (entry.Value >= 0)
                {
                    valueY = y - 12;
                    if (valueY < padding)
                        valueY = ClampInsideBar(y + 15, y, barHeight);
                }
                else
                {
                    valueY = y + barHeight + 15;
                    if (valueY > padding + height)
                        valueY = ClampInsideBar(y + barHeight - 12, y, barHeight);
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