namespace AppSkeleton;

/// <summary>
/// One thing the app does, for a welcome screen: an optional icon in a tile, a title and a line about it, on a card.
/// The colours are the app's to set, with an implicit style.
/// </summary>
public class CFeatureCard : Border
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CFeatureCard), string.Empty);

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CFeatureCard), string.Empty);

    /// <summary>A Material icon's name (the OutlinedIcons font AppSkeleton brings), or empty for none.</summary>
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(CFeatureCard), string.Empty,
            propertyChanged: (b, _, v) => ((CFeatureCard)b)._tile.IsVisible = !string.IsNullOrEmpty((string)v));

    public static readonly BindableProperty CardColorProperty =
        BindableProperty.Create(nameof(CardColor), typeof(Color), typeof(CFeatureCard), Color.FromArgb("#1FFFFFFF"),
            propertyChanged: (b, _, v) => ((CFeatureCard)b).BackgroundColor = (Color)v);

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(CFeatureCard), Color.FromArgb("#33FFFFFF"),
            propertyChanged: (b, _, v) => ((CFeatureCard)b).Stroke = (Color)v);

    public static readonly BindableProperty TitleColorProperty =
        BindableProperty.Create(nameof(TitleColor), typeof(Color), typeof(CFeatureCard), Colors.White);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(CFeatureCard), Color.FromArgb("#B8FFFFFF"));

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(CFeatureCard), Color.FromArgb("#5EE6B8"));

    public static readonly BindableProperty IconBackgroundProperty =
        BindableProperty.Create(nameof(IconBackground), typeof(Color), typeof(CFeatureCard), Color.FromArgb("#335EE6B8"));

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(CFeatureCard), 20d,
            propertyChanged: (b, _, v) => ((CFeatureCard)b).StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = (double)v });

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public Color CardColor { get => (Color)GetValue(CardColorProperty); set => SetValue(CardColorProperty, value); }
    public Color StrokeColor { get => (Color)GetValue(StrokeColorProperty); set => SetValue(StrokeColorProperty, value); }
    public Color TitleColor { get => (Color)GetValue(TitleColorProperty); set => SetValue(TitleColorProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public Color IconColor { get => (Color)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }
    public Color IconBackground { get => (Color)GetValue(IconBackgroundProperty); set => SetValue(IconBackgroundProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    readonly Border _tile;

    public CFeatureCard()
    {
        Padding = 16;
        StrokeThickness = 1;
        BackgroundColor = CardColor;
        Stroke = StrokeColor;
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 };

        var glyph = new Label
        {
            FontFamily = "OutlinedIcons", FontSize = 22,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center,
        };
        glyph.SetBinding(Label.TextProperty, new Binding(nameof(Icon), source: this));
        glyph.SetBinding(Label.TextColorProperty, new Binding(nameof(IconColor), source: this));
        _tile = new Border
        {
            WidthRequest = 42, HeightRequest = 42, StrokeThickness = 0, VerticalOptions = LayoutOptions.Center,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Content = glyph, IsVisible = false, Margin = new Thickness(0, 0, 14, 0),
        };
        _tile.SetBinding(BackgroundColorProperty, new Binding(nameof(IconBackground), source: this));

        var title = new Label { FontFamily = "OpenSansSemibold", FontSize = 17 };
        title.SetBinding(Label.TextProperty, new Binding(nameof(Title), source: this));
        title.SetBinding(Label.TextColorProperty, new Binding(nameof(TitleColor), source: this));
        var text = new Label { FontSize = 13 };
        text.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        text.SetBinding(Label.TextColorProperty, new Binding(nameof(TextColor), source: this));

        var grid = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)] };
        grid.Add(_tile, 0);
        grid.Add(new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { title, text } }, 1);
        Content = grid;
    }
}
