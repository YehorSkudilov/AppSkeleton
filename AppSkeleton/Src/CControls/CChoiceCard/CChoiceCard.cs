using System.Windows.Input;

namespace AppSkeleton;

/// <summary>
/// One answer in a list of choices (an onboarding question, a settings pick): a card with a title, an optional line
/// under it and an optional icon, outlined and tinted while <see cref="IsSelected"/>. A tap runs <see cref="Command"/>
/// (and raises <see cref="Tapped"/>); the list decides what's selected. The colours are the app's to set, with an
/// implicit style.
/// </summary>
public class CChoiceCard : Border
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CChoiceCard), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(CChoiceCard), string.Empty,
            propertyChanged: (b, _, _) => ((CChoiceCard)b).Refresh());

    /// <summary>A Material icon's name (the OutlinedIcons font AppSkeleton brings), or empty for none.</summary>
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(CChoiceCard), string.Empty,
            propertyChanged: (b, _, _) => ((CChoiceCard)b).Refresh());

    public static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(CChoiceCard), false,
            propertyChanged: (b, _, _) => ((CChoiceCard)b).Refresh());

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CChoiceCard));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(CChoiceCard));

    public static readonly BindableProperty CardColorProperty = ColorProperty(nameof(CardColor), "#1AFFFFFF");
    public static readonly BindableProperty SelectedCardColorProperty = ColorProperty(nameof(SelectedCardColor), "#335EE6B8");
    public static readonly BindableProperty StrokeColorProperty = ColorProperty(nameof(StrokeColor), "#00000000");
    public static readonly BindableProperty SelectedStrokeColorProperty = ColorProperty(nameof(SelectedStrokeColor), "#5EE6B8");
    public static readonly BindableProperty TitleColorProperty = ColorProperty(nameof(TitleColor), "#FFFFFF");
    public static readonly BindableProperty SubtitleColorProperty = ColorProperty(nameof(SubtitleColor), "#B8FFFFFF");
    public static readonly BindableProperty IconColorProperty = ColorProperty(nameof(IconColor), "#FFFFFF");
    public static readonly BindableProperty SelectedIconColorProperty = ColorProperty(nameof(SelectedIconColor), "#5EE6B8");

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(CChoiceCard), 16d,
            propertyChanged: (b, _, v) => ((CChoiceCard)b).StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = (double)v });

    public static readonly BindableProperty TitleFontFamilyProperty =
        BindableProperty.Create(nameof(TitleFontFamily), typeof(string), typeof(CChoiceCard), "OpenSansSemibold");

    static BindableProperty ColorProperty(string name, string hex) =>
        BindableProperty.Create(name, typeof(Color), typeof(CChoiceCard), Color.FromArgb(hex),
            propertyChanged: (b, _, _) => ((CChoiceCard)b).Refresh());

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public Color CardColor { get => (Color)GetValue(CardColorProperty); set => SetValue(CardColorProperty, value); }
    public Color SelectedCardColor { get => (Color)GetValue(SelectedCardColorProperty); set => SetValue(SelectedCardColorProperty, value); }
    public Color StrokeColor { get => (Color)GetValue(StrokeColorProperty); set => SetValue(StrokeColorProperty, value); }
    public Color SelectedStrokeColor { get => (Color)GetValue(SelectedStrokeColorProperty); set => SetValue(SelectedStrokeColorProperty, value); }
    public Color TitleColor { get => (Color)GetValue(TitleColorProperty); set => SetValue(TitleColorProperty, value); }
    public Color SubtitleColor { get => (Color)GetValue(SubtitleColorProperty); set => SetValue(SubtitleColorProperty, value); }
    public Color IconColor { get => (Color)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }
    public Color SelectedIconColor { get => (Color)GetValue(SelectedIconColorProperty); set => SetValue(SelectedIconColorProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public string TitleFontFamily { get => (string)GetValue(TitleFontFamilyProperty); set => SetValue(TitleFontFamilyProperty, value); }

    /// <summary>Tapped (after <see cref="Command"/> ran).</summary>
    public event EventHandler? Tapped;

    readonly Label _icon, _title, _subtitle;

    public CChoiceCard()
    {
        Padding = new Thickness(16, 14);
        StrokeThickness = 2;
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 };

        _icon = new Label { FontFamily = "OutlinedIcons", FontSize = 22, VerticalOptions = LayoutOptions.Center, IsVisible = false };
        _title = new Label { FontSize = 16 };
        _title.SetBinding(Label.TextProperty, new Binding(nameof(Title), source: this));
        _title.SetBinding(Label.TextColorProperty, new Binding(nameof(TitleColor), source: this));
        _title.SetBinding(Label.FontFamilyProperty, new Binding(nameof(TitleFontFamily), source: this));
        _subtitle = new Label { FontSize = 13, IsVisible = false };
        _subtitle.SetBinding(Label.TextProperty, new Binding(nameof(Subtitle), source: this));
        _subtitle.SetBinding(Label.TextColorProperty, new Binding(nameof(SubtitleColor), source: this));

        var grid = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)], ColumnSpacing = 0 };
        grid.Add(_icon, 0);
        grid.Add(new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { _title, _subtitle } }, 1);
        Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);
        Refresh();
    }

    async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Command?.CanExecute(CommandParameter) != false)
            Command?.Execute(CommandParameter);
        Tapped?.Invoke(this, EventArgs.Empty);
        this.AbortAnimation("CChoiceCardPress");
        await this.ScaleToAsync(0.97, 60, Easing.CubicOut);
        await this.ScaleToAsync(1, 140, Easing.SpringOut);
    }

    void Refresh()
    {
        // Not set up yet (a property set by a style before the constructor's body ran).
        if (_icon == null)
            return;
        BackgroundColor = IsSelected ? SelectedCardColor : CardColor;
        Stroke = IsSelected ? SelectedStrokeColor : StrokeColor;
        _icon.Text = Icon;
        _icon.IsVisible = !string.IsNullOrEmpty(Icon);
        _icon.TextColor = IsSelected ? SelectedIconColor : IconColor;
        _icon.Margin = _icon.IsVisible ? new Thickness(0, 0, 14, 0) : 0;
        _subtitle.IsVisible = !string.IsNullOrEmpty(Subtitle);
        SemanticProperties.SetDescription(this, string.IsNullOrEmpty(Subtitle) ? Title : $"{Title}. {Subtitle}");
    }
}
