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

        float max = Entries.Max(e => e.Value);
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

            for (int i = 0; i < count; i++)
            {
                var entry = Entries[i];

                float barHeight = (entry.Value / max) * height;

                float x = padding + i * (barWidth + spacing);
                float y = padding + (height - barHeight);

                // BAR
                canvas.FillColor = entry.Color ?? Colors.DodgerBlue;
                canvas.FillRectangle(x, y, barWidth, barHeight);

                // VALUE LABEL (above or inside safely)
                canvas.FontColor = Colors.White;

                float valueY = y - 12;
                if (valueY < padding)
                    valueY = y + 15;

                canvas.DrawString(
                    entry.Value.ToString("0"),
                    x + barWidth / 2,
                    valueY,
                    HorizontalAlignment.Center);

                // LABEL (below bar with proper gap)
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

                float barWidth = (entry.Value / max) * width;

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
                    entry.Value.ToString("0"),
                    valueX,
                    y + barHeight / 2,
                    HorizontalAlignment.Left);
            }
        }
    }
}