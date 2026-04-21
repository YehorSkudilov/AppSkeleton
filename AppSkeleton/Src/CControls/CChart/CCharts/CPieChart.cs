using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AppSkeleton;

public class CPieChart : BindableObject, IDrawable
{
    public event Action? RedrawRequested;

    // -------------------------
    // VALUES
    // -------------------------
    public static readonly BindableProperty ValuesProperty =
        BindableProperty.Create(
            nameof(Values),
            typeof(ObservableCollection<float>),
            typeof(CPieChart),
            new ObservableCollection<float>(),
            propertyChanged: OnValuesChanged);

    public ObservableCollection<float> Values
    {
        get => (ObservableCollection<float>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    private static void OnValuesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var chart = (CPieChart)bindable;

        if (oldValue is ObservableCollection<float> oldCol)
            oldCol.CollectionChanged -= chart.OnCollectionChanged;

        if (newValue is ObservableCollection<float> newCol)
            newCol.CollectionChanged += chart.OnCollectionChanged;

        chart.RequestRedraw();
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RequestRedraw();
    }

    // -------------------------
    // REDRAW SYSTEM
    // -------------------------
    private void RequestRedraw()
    {
        RedrawRequested?.Invoke();
    }

    // -------------------------
    // DRAW
    // -------------------------
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values == null || Values.Count == 0)
            return;

        float total = Values.Sum();
        if (total <= 0)
            return;

        float padding = 20;

        float size = Math.Min(dirtyRect.Width, dirtyRect.Height) - (padding * 2);
        float centerX = dirtyRect.Width / 2;
        float centerY = dirtyRect.Height / 2;

        float radius = size / 2;

        float startAngle = -90f; // start at top

        var colors = new[]
        {
            Colors.DodgerBlue,
            Colors.Orange,
            Colors.MediumSeaGreen,
            Colors.MediumPurple,
            Colors.IndianRed,
            Colors.Gold,
            Colors.CornflowerBlue
        };

        for (int i = 0; i < Values.Count; i++)
        {
            float value = Values[i];
            float sweep = (value / total) * 360f;

            var color = colors[i % colors.Length];

            canvas.FillColor = color;

            // Build slice path
            var path = new PathF();
            path.MoveTo(centerX, centerY);

            float endAngle = startAngle + sweep;

            AddArcToPath(path, centerX, centerY, radius, startAngle, endAngle);

            path.Close();

            canvas.FillPath(path);

            // LABEL
            float midAngle = startAngle + sweep / 2f;
            float rad = midAngle * (float)Math.PI / 180f;

            float labelX = centerX + (radius * 0.6f) * (float)Math.Cos(rad);
            float labelY = centerY + (radius * 0.6f) * (float)Math.Sin(rad);

            canvas.FontColor = Colors.White;
            canvas.FontSize = 12;

            canvas.DrawString(
                Values[i].ToString("0"),
                labelX,
                labelY,
                HorizontalAlignment.Center);

            startAngle += sweep;
        }
    }

    // -------------------------
    // ARC HELPER
    // -------------------------
    private void AddArcToPath(PathF path, float cx, float cy, float r, float startAngle, float endAngle)
    {
        int segments = 20; // smoothness
        float angleStep = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + i * angleStep) * (float)Math.PI / 180f;

            float x = cx + r * (float)Math.Cos(angle);
            float y = cy + r * (float)Math.Sin(angle);

            if (i == 0)
                path.LineTo(x, y);
            else
                path.LineTo(x, y);
        }
    }
}