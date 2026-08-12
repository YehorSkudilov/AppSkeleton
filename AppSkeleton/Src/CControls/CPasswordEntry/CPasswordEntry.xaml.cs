namespace AppSkeleton;

// Wraps a password Entry with a show/hide toggle - one place owning that Grid+Label
// pair and its toggle logic instead of it being duplicated at every password field.
public partial class CPasswordEntry : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CPasswordEntry), string.Empty, BindingMode.TwoWay);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(CPasswordEntry));

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty PlaceholderColorProperty =
        BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(CPasswordEntry), Color.FromArgb("#88ffffff"));

    public Color PlaceholderColor
    {
        get => (Color)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(CPasswordEntry), Colors.White);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty ReturnTypeProperty =
        BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(CPasswordEntry), ReturnType.Default);

    public ReturnType ReturnType
    {
        get => (ReturnType)GetValue(ReturnTypeProperty);
        set => SetValue(ReturnTypeProperty, value);
    }

    public event EventHandler? Completed;

    public CPasswordEntry()
    {
        InitializeComponent();
        MainGrid.BindingContext = this;
    }

    private void OnEntryCompleted(object sender, EventArgs e) => Completed?.Invoke(this, e);

    private void OnToggleVisibility(object sender, EventArgs e)
    {
        PasswordInput.IsPassword = !PasswordInput.IsPassword;
        VisibilityToggle.Icon = PasswordInput.IsPassword ? "visibility" : "visibility_off";
    }

    // Shadows VisualElement.Focus/Unfocus rather than overriding (neither is virtual) -
    // callers hold this as a CPasswordEntry, not a bare VisualElement, so the shadow
    // resolves correctly and forwards to the actual input instead of focusing this
    // ContentView itself (which can't receive focus).
    public new bool Focus() => PasswordInput.Focus();

    public new void Unfocus() => PasswordInput.Unfocus();

 
}
