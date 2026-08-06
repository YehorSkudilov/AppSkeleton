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
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        Applied?.Invoke(this, EventArgs.Empty);
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        Reset?.Invoke(this, EventArgs.Empty);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private async void OnBackgroundTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
