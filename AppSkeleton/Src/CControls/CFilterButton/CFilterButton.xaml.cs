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

    // "filter_list" doesn't exist in RegularIcons at all (verified against the actual
    // font file) - only in OutlinedIcons, which is also what the modal's close icon
    // uses, so this was rendering as a missing/blank glyph the whole time.
    public static readonly BindableProperty IconFontFamilyProperty =
        BindableProperty.Create(nameof(IconFontFamily), typeof(string), typeof(CFilterButton), "OutlinedIcons");

    public string IconFontFamily
    {
        get => (string)GetValue(IconFontFamilyProperty);
        set => SetValue(IconFontFamilyProperty, value);
    }

    // -1 is MAUI's "no explicit size requested" sentinel (the same default View.HeightRequest/
    // WidthRequest already use) - left unset, CButton's icon Image sizes itself to fill
    // whatever space CFilterButton itself is given, rather than being pinned to a fixed
    // pixel size that wouldn't suit every page this is dropped into. Still fully
    // overridable by a consumer that wants a specific size.
    public static readonly BindableProperty IconHeightRequestProperty =
        BindableProperty.Create(nameof(IconHeightRequest), typeof(double), typeof(CFilterButton), -1.0);

    public double IconHeightRequest
    {
        get => (double)GetValue(IconHeightRequestProperty);
        set => SetValue(IconHeightRequestProperty, value);
    }

    public static readonly BindableProperty IconWidthRequestProperty =
        BindableProperty.Create(nameof(IconWidthRequest), typeof(double), typeof(CFilterButton), -1.0);

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

    // Caps the popup card's size directly - MaximumHeightRequest/WidthRequest are
    // enforced by MAUI's own cross-platform layout as a hard ceiling on the arranged
    // size, independent of whatever the native AcrylicView measurement reports, so
    // this is a reliable way to bound it regardless of that.
    public static readonly BindableProperty ContentHeightRequestProperty =
        BindableProperty.Create(nameof(ContentHeightRequest), typeof(double), typeof(CFilterButton), -1.0);

    public double ContentHeightRequest
    {
        get => (double)GetValue(ContentHeightRequestProperty);
        set => SetValue(ContentHeightRequestProperty, value);
    }

    public static readonly BindableProperty ContentWidthRequestProperty =
        BindableProperty.Create(nameof(ContentWidthRequest), typeof(double), typeof(CFilterButton), 280.0);

    public double ContentWidthRequest
    {
        get => (double)GetValue(ContentWidthRequestProperty);
        set => SetValue(ContentWidthRequestProperty, value);
    }

    // Raised when the user taps Apply - the consumer reads whatever state its
    // FilterContent controls hold (e.g. bound DatePicker values) and acts on it.
    public event EventHandler? Applied;

    // Raised on Cancel or backdrop tap - consumer should discard any pending changes.
    public event EventHandler? Cancelled;

    // Raised when the user taps Reset - consumer should restore its filter state to
    // defaults (e.g. reset bound DatePicker values) and typically re-apply immediately.
    public event EventHandler? Reset;

    public CFilterButton()
    {
        InitializeComponent();
    }

    private async void OnButtonClicked(object sender, EventArgs e)
    {
        var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navigation == null || FilterContent == null)
            return;

        var buttonCenter = GetAbsoluteCenter(this);

        var modal = new CFilterModalPage(Title, FilterContent, ContentHeightRequest, ContentWidthRequest, buttonCenter)
        {
            // FilterContent gets reparented onto this new, otherwise-unrelated page,
            // which would leave its {Binding ...} expressions unresolved unless the
            // BindingContext they depend on (the page CFilterButton is embedded in)
            // is explicitly carried over too.
            BindingContext = BindingContext
        };
        modal.Applied += (_, _) => Applied?.Invoke(this, EventArgs.Empty);
        modal.Cancelled += (_, _) => Cancelled?.Invoke(this, EventArgs.Empty);
        modal.Reset += (_, _) => Reset?.Invoke(this, EventArgs.Empty);

        // animated: false - the modal page runs its own grow/shrink animation
        // (see CFilterModalPage), so the platform's own push transition is skipped.
        await navigation.PushModalAsync(modal, animated: false);
    }

    // Sums each ancestor's own position within its parent, up to the root page - MAUI
    // has no single built-in "give me my absolute screen position" API, but each
    // VisualElement's X/Y (its Bounds) are already relative-to-parent post-layout, so
    // walking the chain and adding them up gives the absolute position. Doesn't account
    // for rotation/scale transforms on an ancestor, but nothing in this app's layouts
    // applies those, so it's a reliable enough approximation here. Shared with
    // CFilterModalPage, which uses it to locate the card the same way.
    internal static Point GetAbsolutePosition(VisualElement element)
    {
        double x = 0, y = 0;

        Element? current = element;
        while (current is VisualElement ve)
        {
            x += ve.X;
            y += ve.Y;
            current = current.Parent;
        }

        return new Point(x, y);
    }

    internal static Point GetAbsoluteCenter(VisualElement element)
    {
        var position = GetAbsolutePosition(element);
        return new Point(position.X + element.Width / 2, position.Y + element.Height / 2);
    }
}
