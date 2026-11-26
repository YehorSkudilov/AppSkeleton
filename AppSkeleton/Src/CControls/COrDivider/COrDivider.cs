namespace AppSkeleton;

/// <summary>
/// A line, a word, a line: "—— or ——" between a form and the buttons under it (e.g. email sign-in, then Google).
/// Style it from the app with an implicit style setting <see cref="LineColor"/> and <see cref="TextColor"/>.
/// </summary>
public class COrDivider : Grid
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(COrDivider), "or");

    public static readonly BindableProperty LineColorProperty =
        BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(COrDivider), Color.FromArgb("#33FFFFFF"));

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(COrDivider), Color.FromArgb("#B8FFFFFF"));

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(COrDivider), 13d);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public Color LineColor { get => (Color)GetValue(LineColorProperty); set => SetValue(LineColorProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    public COrDivider()
    {
        ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto), new(GridLength.Star)];
        ColumnSpacing = 12;
        this.Add(Line(), 0);
        var label = new Label { VerticalOptions = LayoutOptions.Center };
        label.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        label.SetBinding(Label.TextColorProperty, new Binding(nameof(TextColor), source: this));
        label.SetBinding(Label.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        this.Add(label, 1);
        this.Add(Line(), 2);
    }

    // BackgroundColor rather than Color: apps often have an implicit BoxView style that sets Color.
    BoxView Line()
    {
        var line = new BoxView { HeightRequest = 1, Color = Colors.Transparent, VerticalOptions = LayoutOptions.Center };
        line.SetBinding(BackgroundColorProperty, new Binding(nameof(LineColor), source: this));
        return line;
    }
}
