namespace AppSkeleton;

// Universal filter trigger - one drop-in component combining the icon button and its
// popup, reusable anywhere a modal filter UI is needed. The specific filter controls
// (date pickers, dropdowns, etc.) are supplied by the consumer via FilterContent; this
// control only owns the button chrome and the modal Apply/Cancel handshake.
public partial class CFilterButton : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(CFilterButton), "filter_list");

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty IconFontFamilyProperty =
        BindableProperty.Create(nameof(IconFontFamily), typeof(string), typeof(CFilterButton), "RegularIcons");

    public string IconFontFamily
    {
        get => (string)GetValue(IconFontFamilyProperty);
        set => SetValue(IconFontFamilyProperty, value);
    }

    public static readonly BindableProperty IconHeightRequestProperty =
        BindableProperty.Create(nameof(IconHeightRequest), typeof(double), typeof(CFilterButton), 10.0);

    public double IconHeightRequest
    {
        get => (double)GetValue(IconHeightRequestProperty);
        set => SetValue(IconHeightRequestProperty, value);
    }

    public static readonly BindableProperty IconWidthRequestProperty =
        BindableProperty.Create(nameof(IconWidthRequest), typeof(double), typeof(CFilterButton), 10.0);

    public double IconWidthRequest
    {
        get => (double)GetValue(IconWidthRequestProperty);
        set => SetValue(IconWidthRequestProperty, value);
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CFilterButton), "Filter");

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty FilterContentProperty =
        BindableProperty.Create(nameof(FilterContent), typeof(View), typeof(CFilterButton));

    public View FilterContent
    {
        get => (View)GetValue(FilterContentProperty);
        set => SetValue(FilterContentProperty, value);
    }

    // Raised when the user taps Apply - the consumer reads whatever state its
    // FilterContent controls hold (e.g. bound DatePicker values) and acts on it.
    public event EventHandler? Applied;

    // Raised on Cancel or backdrop tap - consumer should discard any pending changes.
    public event EventHandler? Cancelled;

    public CFilterButton()
    {
        InitializeComponent();
    }

    private async void OnButtonClicked(object sender, EventArgs e)
    {
        var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navigation == null || FilterContent == null)
            return;

        var modal = new CFilterModalPage(Title, FilterContent)
        {
            // FilterContent gets reparented onto this new, otherwise-unrelated page,
            // which would leave its {Binding ...} expressions unresolved unless the
            // BindingContext they depend on (the page CFilterButton is embedded in)
            // is explicitly carried over too.
            BindingContext = BindingContext
        };
        modal.Applied += (_, _) => Applied?.Invoke(this, EventArgs.Empty);
        modal.Cancelled += (_, _) => Cancelled?.Invoke(this, EventArgs.Empty);

        await navigation.PushModalAsync(modal);
    }
}
