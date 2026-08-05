using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Linq;

namespace AppSkeleton;

public class CSegmentedBarChart : BindableObject, IDrawable
{
    public event Action? RedrawRequested;

    // -------------------------
    // ENTRIES
    // -------------------------
    public static readonly BindableProperty EntriesProperty =
        BindableProperty.Create(
            nameof(Entries),
            typeof(ObservableCollection<CChartEntry>),
            typeof(CSegmentedBarChart),
            new ObservableCollection<CChartEntry>(),
            propertyChanged: (b, o, n) => ((CSegmentedBarChart)b).RequestRedraw());

    public ObservableCollection<CChartEntry> Entries
    {
        get => (ObservableCollection<CChartEntry>)GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    // -------------------------
    // ORIENTATION
    // -------------------------
    public static readonly BindableProperty IsHorizontalProperty =
        BindableProperty.Create(
            nameof(IsHorizontal),
            typeof(bool),
            typeof(CSegmentedBarChart),
            true,
            propertyChanged: (b, o, n) => ((CSegmentedBarChart)b).RequestRedraw());

    public bool IsHorizontal
    {
        get => (bool)GetValue(IsHorizontalProperty);
        set => SetValue(IsHorizontalProperty, value);
    }

    // -------------------------
    // CORNER RADIUS
    // -------------------------
    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(
            nameof(CornerRadius),
            typeof(float),
            typeof(CSegmentedBarChart),
            0f,
            propertyChanged: (b, o, n) => ((CSegmentedBarChart)b).RequestRedraw());

    public float CornerRadius
    {
        get => (float)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private void RequestRedraw() => RedrawRequested?.Invoke();

    // -------------------------
    // DRAW
    // -------------------------
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Entries == null || Entries.Count == 0)
            return;

        float padding = 8;
        float labelSize = 14;
        float labelGap = 6;

        float total = Entries.Sum(e => e.Value);
        if (total <= 0) return;

        canvas.FontSize = 12;

        // =====================================================
        // HORIZONTAL
        // =====================================================
        if (IsHorizontal)
        {
            float width = dirtyRect.Width - padding * 2;
            float height = dirtyRect.Height - padding * 2 - labelSize;

            float barHeight = height;
            float barY = padding;

            float startX = padding;


                // ---------------- PER SEGMENT MODE ----------------
                foreach (var entry in Entries)
                {
                    float segmentWidth = (entry.Value / total) * width;

                    var rect = new RectF(startX, barY, segmentWidth, barHeight);

                    canvas.FillColor = entry.Color ?? Colors.DodgerBlue;

                    canvas.FillRoundedRectangle(rect, CornerRadius);

                    if (segmentWidth > 20)
                    {
                        canvas.FontColor = Colors.White;

                        canvas.DrawString(
                            $"{entry.Value:0}",
                            rect,
                            HorizontalAlignment.Center,
                            VerticalAlignment.Center);
                    }

                    startX += segmentWidth;
                }
            

            // ---------------- LABELS ----------------
            float labelY = barY + barHeight + labelGap;
            startX = padding;

            foreach (var entry in Entries)
            {
                float segmentWidth = (entry.Value / total) * width;

                canvas.FontColor = Colors.White;

                canvas.DrawString(
                    entry.Label,
                    startX,
                    labelY,
                    segmentWidth,
                    labelSize,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Top);

                startX += segmentWidth;
            }
        }

        // =====================================================
        // VERTICAL
        // =====================================================
        else
        {
            float width = dirtyRect.Width - padding * 2;
            float height = dirtyRect.Height - padding * 2 - labelSize;

            float barWidth = width * 0.6f;
            float x = padding + (width - barWidth) / 2;

            float yBottom = padding + height;
            float currentY = yBottom;

         
                foreach (var entry in Entries)
                {
                    float segmentHeight = (entry.Value / total) * height;

                    var rect = new RectF(x, currentY - segmentHeight, barWidth, segmentHeight);

                    canvas.FillColor = entry.Color ?? Colors.DodgerBlue;

                    canvas.FillRoundedRectangle(rect, CornerRadius);

                    if (segmentHeight > 18)
                    {
                        canvas.FontColor = Colors.White;

                        canvas.DrawString(
                            $"{entry.Value:0}",
                            rect,
                            HorizontalAlignment.Center,
                            VerticalAlignment.Center);
                    }

                    currentY -= segmentHeight;
                }
            

            canvas.FontColor = Colors.Gray;

            canvas.DrawString(
                string.Join("  ", Entries.Select(e => e.Label)),
                padding,
                yBottom + labelGap,
                width,
                labelSize,
                HorizontalAlignment.Center,
                VerticalAlignment.Top);
        }
    }
}