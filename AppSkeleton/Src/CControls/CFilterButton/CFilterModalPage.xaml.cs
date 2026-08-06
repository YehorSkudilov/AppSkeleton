namespace AppSkeleton;

// Internal to CFilterButton - a modal page is what actually gets a true full-screen
// overlay regardless of how small the triggering button is, which a ContentView
// confined to its parent's layout bounds can't do without extra plumbing.
public partial class CFilterModalPage : ContentPage
{
    public event EventHandler? Applied;
    public event EventHandler? Cancelled;
    public event EventHandler? Reset;

    public CFilterModalPage(string title, View filterContent)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        ContentHost.Content = filterContent;

        // Anchored at the same top-right corner the card docks to, so scaling in/out
        // from there reads as "grows out of / shrinks back into" the header icon that
        // opened it, rather than an unrelated modal sheet animation.
        CardAcrylic.AnchorX = 1;
        CardAcrylic.AnchorY = 0;
        CardAcrylic.Scale = 0.1;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CardAcrylic.ScaleTo(1, 200, Easing.CubicOut);
    }

    private async Task CloseAsync()
    {
        await CardAcrylic.ScaleTo(0.1, 150, Easing.CubicIn);
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
