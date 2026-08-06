namespace AppSkeleton;

// Internal to CFilterButton - a modal page is what actually gets a true full-screen
// overlay regardless of how small the triggering button is, which a ContentView
// confined to its parent's layout bounds can't do without extra plumbing.
public partial class CFilterModalPage : ContentPage
{
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
        Point buttonCenter)
    {
        InitializeComponent();

        this.buttonCenter = buttonCenter;

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
    }

    // Deliberately not gated behind CardAcrylic.SizeChanged - AcrylicView's native
    // measurement has been unreliable throughout this control's development (see the
    // BorderViewGroup/AcrylicViewHandler fixes elsewhere in this project), so waiting
    // on it firing with a valid size risked the scale-in animation silently never
    // running at all, leaving the popup stuck at Scale 0.05. OnAppearing always fires
    // once the page is actually shown, so the animation is guaranteed to run either
    // way - ApplyAnchor() falls back to a fixed corner if the card's real bounds
    // genuinely aren't available yet.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (hasAnimatedIn)
            return;

        hasAnimatedIn = true;

        ApplyAnchor();
        await CardAcrylic.ScaleTo(1, 200, Easing.CubicOut);
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
        await CardAcrylic.ScaleTo(0.05, 150, Easing.CubicIn);
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
