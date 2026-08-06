namespace AppSkeleton;

// Internal to CFilterButton - a modal page is what actually gets a true full-screen
// overlay regardless of how small the triggering button is, which a ContentView
// confined to its parent's layout bounds can't do without extra plumbing.
public partial class CFilterModalPage : ContentPage
{
    // Mirrors CFilterButton's own Icon/IconFontFamily/IconHeightRequest/IconWidthRequest
    // bindable properties so the close button is customizable the same way the trigger
    // button is, via CFilterButton.CloseIcon/CloseIconFontFamily/etc.
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(CFilterModalPage), "close");

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty IconFontFamilyProperty =
        BindableProperty.Create(nameof(IconFontFamily), typeof(string), typeof(CFilterModalPage), "OutlinedIcons");

    public string IconFontFamily
    {
        get => (string)GetValue(IconFontFamilyProperty);
        set => SetValue(IconFontFamilyProperty, value);
    }

    public event EventHandler? Applied;
    public event EventHandler? Cancelled;
    public event EventHandler? Reset;

    private readonly Point buttonCenter;
    private bool hasAnimatedIn;

    public CFilterModalPage(
        string title,
        View filterContent,
        double contentHeightRequest,
        double contentWidthRequest,
        Point buttonCenter,
        string closeIcon,
        string closeIconFontFamily)
    {
        InitializeComponent();

        this.buttonCenter = buttonCenter;

        Icon = closeIcon;
        IconFontFamily = closeIconFontFamily;


        TitleLabel.Text = title;
        ContentHost.Content = filterContent;

        CardAcrylic.WidthRequest = contentWidthRequest;

        // -1 (CFilterButton.ContentHeightRequest's default) means "no cap". Maximum*Request
        // is enforced by MAUI's own cross-platform layout as a hard ceiling on the final
        // arranged size, independent of whatever the native AcrylicView measurement
        // reports - a reliable way to bound this regardless of that.
        if (contentHeightRequest > 0)
            CardAcrylic.MaximumHeightRequest = contentHeightRequest;

        CardAcrylic.Scale = 0.05;
        CardAcrylic.Opacity = 0;
    }

    // OnAppearing can fire before the native view has actually completed its first
    // layout/paint pass (particularly right after PushModalAsync(animated: false),
    // which skips the platform's own transition entirely) - starting the animation
    // that early lets it run and finish before anything is ever drawn, so the popup
    // just snaps straight to its final state with no visible transition. Not gated
    // behind CardAcrylic.SizeChanged directly, though - AcrylicView's native
    // measurement has been unreliable throughout this control's development (see the
    // BorderViewGroup/AcrylicViewHandler fixes elsewhere in this project) and that
    // event risked never firing at all. Poll for a real size instead, bounded so a
    // genuinely-stuck layout still falls through to ApplyAnchor's own corner fallback
    // and the animation runs anyway rather than hanging.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (hasAnimatedIn)
            return;

        hasAnimatedIn = true;

        for (var i = 0; i < 20 && CardAcrylic.Width <= 0; i++)
            await Task.Delay(16);

        ApplyAnchor();
        await Task.WhenAll(
            CardAcrylic.ScaleToAsync(1, 200, Easing.CubicOut),
            CardAcrylic.FadeToAsync(1, 150, Easing.CubicOut));
    }

    private void ApplyAnchor()
    {
        if (CardAcrylic.Width > 0 && CardAcrylic.Height > 0)
        {
            var cardPosition = CFilterButton.GetAbsolutePosition(CardAcrylic);

            // Where the button's center falls within the card's own bounds, as a
            // normalized [0,1] fraction - AnchorX/AnchorY scale around that point, so
            // the grow/shrink animation reads as coming from (and returning to) the
            // button's actual screen position rather than an arbitrary fixed corner.
            // Clamped since the button can sit outside the card's bounds (e.g. above
            // it), which would otherwise anchor past the card's own edge.
            var anchorX = (buttonCenter.X - cardPosition.X) / CardAcrylic.Width;
            var anchorY = (buttonCenter.Y - cardPosition.Y) / CardAcrylic.Height;

            CardAcrylic.AnchorX = Math.Clamp(anchorX, 0, 1);
            CardAcrylic.AnchorY = Math.Clamp(anchorY, 0, 1);
        }
        else
        {
            // The card's real bounds aren't resolved yet - fall back to the corner it
            // visually docks to, rather than skipping the animation entirely.
            CardAcrylic.AnchorX = 1;
            CardAcrylic.AnchorY = 0;
        }
    }

    private async Task CloseAsync()
    {
        // Opacity finishes a touch faster than scale - fading fully out while still
        // mid-shrink reads as the card dissolving into the trigger button, rather than
        // a flat scale-down that leaves a tiny fully-opaque dot for a frame.
        await Task.WhenAll(
            CardAcrylic.ScaleToAsync(0.05, 150, Easing.CubicIn),
            CardAcrylic.FadeToAsync(0, 120, Easing.CubicIn));

        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        await CloseAsync();
        Applied?.Invoke(this, EventArgs.Empty);
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        await CloseAsync();
        Reset?.Invoke(this, EventArgs.Empty);
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await CloseAsync();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private async void OnBackgroundTapped(object sender, EventArgs e)
    {
        await CloseAsync();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
