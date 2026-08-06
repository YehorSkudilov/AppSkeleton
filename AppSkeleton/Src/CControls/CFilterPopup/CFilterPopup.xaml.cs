namespace AppSkeleton;

// Universal filter popup shell - reusable anywhere a modal filter UI is needed.
// The specific filter controls (date pickers, dropdowns, etc.) are supplied by the
// consumer via FilterContent; this control only owns the open/close chrome and the
// Apply/Cancel handshake.
public partial class CFilterPopup : ContentView
{
    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(
            nameof(IsOpen),
            typeof(bool),
            typeof(CFilterPopup),
            false,
            BindingMode.TwoWay);

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(CFilterPopup),
            "Filter");

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty FilterContentProperty =
        BindableProperty.Create(
            nameof(FilterContent),
            typeof(View),
            typeof(CFilterPopup),
            null,
            propertyChanged: OnFilterContentChanged);

    public View FilterContent
    {
        get => (View)GetValue(FilterContentProperty);
        set => SetValue(FilterContentProperty, value);
    }

    private static void OnFilterContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var popup = (CFilterPopup)bindable;
        popup.ContentHost.Content = newValue as View;
    }

    // Raised when the user taps Apply - the consumer reads whatever state its
    // FilterContent controls hold (e.g. bound DatePicker values) and acts on it.
    public event EventHandler? Applied;

    // Raised on Cancel or backdrop tap - consumer should discard any pending changes.
    public event EventHandler? Cancelled;

    public CFilterPopup()
    {
        InitializeComponent();
    }

    private void OnApplyClicked(object sender, EventArgs e)
    {
        IsOpen = false;
        Applied?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        IsOpen = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        IsOpen = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
