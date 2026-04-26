namespace AppSkeleton;

public partial class CButton : ContentView
{
    // ---------------- ICON ----------------
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(CButton));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // ---------------- SIZE ----------------
    public static readonly BindableProperty IconHeightRequestProperty =
        BindableProperty.Create(nameof(IconHeightRequest), typeof(double), typeof(CButton), 24.0);

    public double IconHeightRequest
    {
        get => (double)GetValue(IconHeightRequestProperty);
        set => SetValue(IconHeightRequestProperty, value);
    }

    public static readonly BindableProperty IconWidthRequestProperty =
        BindableProperty.Create(nameof(IconWidthRequest), typeof(double), typeof(CButton), 24.0);

    public double IconWidthRequest
    {
        get => (double)GetValue(IconWidthRequestProperty);
        set => SetValue(IconWidthRequestProperty, value);
    }

    // ---------------- PADDING ----------------
    public static readonly BindableProperty IconPaddingProperty =
        BindableProperty.Create(nameof(IconPadding), typeof(Thickness), typeof(CButton), new Thickness(0));

    public Thickness IconPadding
    {
        get => (Thickness)GetValue(IconPaddingProperty);
        set => SetValue(IconPaddingProperty, value);
    }

    // ---------------- FONT ----------------
    public static readonly BindableProperty IconFontFamilyProperty =
        BindableProperty.Create(
            nameof(IconFontFamily),
            typeof(string),
            typeof(CButton),
            default(string),
            propertyChanged: OnFontFamilyChanged);

    public string IconFontFamily
    {
        get => (string)GetValue(IconFontFamilyProperty);
        set => SetValue(IconFontFamilyProperty, value);
    }

    public static readonly BindableProperty SelectedIconFontFamilyProperty =
    BindableProperty.Create(nameof(SelectedIconFontFamily), typeof(string), typeof(CButton));

    public string SelectedIconFontFamily
    {
        get => (string)GetValue(SelectedIconFontFamilyProperty);
        set => SetValue(SelectedIconFontFamilyProperty, value);
    }

    public static readonly BindableProperty CurrentFontFamilyProperty =
        BindableProperty.Create(nameof(CurrentFontFamily), typeof(string), typeof(CButton));

    public static readonly BindableProperty CButtonBackgroundColorProperty =
        BindableProperty.Create(
            nameof(CButtonBackgroundColor),
            typeof(Color),
            typeof(CButton),
            Color.FromArgb("#20ffffff"));

    public Color CButtonBackgroundColor
    {
        get => (Color)GetValue(CButtonBackgroundColorProperty);
        set => SetValue(CButtonBackgroundColorProperty, value);
    }

    public static readonly BindableProperty CButtonStrokeProperty =
    BindableProperty.Create(
        nameof(CButtonStroke),
        typeof(Color),
        typeof(CButton),
       Color.FromArgb("#40ffffff"));

    public Color CButtonStroke
    {
        get => (Color)GetValue(CButtonStrokeProperty);
        set => SetValue(CButtonStrokeProperty, value);
    }

    public string CurrentFontFamily
    {
        get => (string)GetValue(CurrentFontFamilyProperty);
        set => SetValue(CurrentFontFamilyProperty, value);
    }

    public static readonly BindableProperty IsSelectableProperty =
    BindableProperty.Create(nameof(IsSelectable), typeof(bool), typeof(CButton), true);

    public bool IsSelectable
    {
        get => (bool)GetValue(IsSelectableProperty);
        set => SetValue(IsSelectableProperty, value);
    }

    // ---------------- ACTION (NEW) ----------------
    public static readonly BindableProperty ClickedActionProperty =
    BindableProperty.Create(nameof(ClickedAction), typeof(Action), typeof(CButton));

    public Action? ClickedAction
    {
        get => (Action?)GetValue(ClickedActionProperty);
        set => SetValue(ClickedActionProperty, value);
    }

    public static readonly BindableProperty IsBackgroundVisibleProperty =
BindableProperty.Create(nameof(IsBackgroundVisible), typeof(bool), typeof(CButton), true);

    public bool IsBackgroundVisible
    {
        get => (bool)GetValue(IsBackgroundVisibleProperty);
        set => SetValue(IsBackgroundVisibleProperty, value);
    }

    public CButton()
    {
        InitializeComponent();
        MainGrid.BindingContext = this;
    }

    // ---------------- TAP HANDLER ----------------
    private void OnTapped(object sender, TappedEventArgs e)
    {
        ClickedAction?.Invoke();
        Select(true);
    }

    public void Select(bool select)
    {
        if (IsSelectable)
        {
            if (select && SelectedIconFontFamily != null)
            {
                CurrentFontFamily = SelectedIconFontFamily;
            }
            else if (IconFontFamily != null)
            {
                CurrentFontFamily = IconFontFamily;
            }
        }
    }

    private void MainGrid_Loaded(object sender, EventArgs e)
    {
        CurrentFontFamily = IconFontFamily;

    }

    private static void OnFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CButton button)
        {
            button.CurrentFontFamily = button.IconFontFamily;
        }
    }
}