namespace AppSkeleton;

public partial class CButton : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CButton));


    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty =
    BindableProperty.Create(
        nameof(CornerRadius),
        typeof(float),
        typeof(CButton),
        50f);

    public float CornerRadius
    {
        get => (float)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

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

    // ---------------- ICON COLOR ----------------
    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(CButton), Colors.White);

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
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
    public event EventHandler? Clicked;



    public static readonly BindableProperty IsBackgroundVisibleProperty =
BindableProperty.Create(nameof(IsBackgroundVisible), typeof(bool), typeof(CButton), true);

    public bool IsBackgroundVisible
    {
        get => (bool)GetValue(IsBackgroundVisibleProperty);
        set => SetValue(IsBackgroundVisibleProperty, value);
    }

    // AcrylicView's blur/measurement machinery is overkill (and, in some layout
    // contexts, buggy - see CFilterModalPage) for a plain small icon button. Set this
    // false to fall back to a flat Border using the same CButtonStroke/
    // CButtonBackgroundColor colors, no blur.
    public static readonly BindableProperty UseAcrylicBackgroundProperty =
        BindableProperty.Create(nameof(UseAcrylicBackground), typeof(bool), typeof(CButton), true);

    public bool UseAcrylicBackground
    {
        get => (bool)GetValue(UseAcrylicBackgroundProperty);
        set => SetValue(UseAcrylicBackgroundProperty, value);
    }

    public CButton()
    {
        InitializeComponent();
        MainGrid.BindingContext = this;

#if ANDROID
        // HandlerChanged (not MainGrid_Loaded) is the reliable "native view now exists"
        // hook - Loaded isn't guaranteed to fire for elements inside a ContentView.
        MainGrid.HandlerChanged += (_, _) => ApplyAndroidRipple();
#endif
    }

    // ---------------- TAP HANDLER ----------------
    private void OnTapped(object sender, TappedEventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
        Select(true);

#if ANDROID
        _ = AnimateAndroidRippleAsync();
#endif
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

#if ANDROID
    // Gives MainGrid the same native Material ripple every regular Android button has -
    // a real platform-drawn wave from the touch point, not a hand-rolled animation (which
    // is why it's smooth/never laggy: the OS renders it, not MAUI).
    private void ApplyAndroidRipple()
    {
        try
        {
            if (MainGrid.Handler?.PlatformView is not Android.Views.View nativeView)
                return;

            var context = nativeView.Context;
            if (context == null)
                return;

            var outValue = new Android.Util.TypedValue();
            var resolved = context.Theme.ResolveAttribute(
                Android.Resource.Attribute.SelectableItemBackground, outValue, true);

            // Theme attribute resolution can silently fail depending on the active theme -
            // fall back to a manually-built ripple so there's always something to see.
            nativeView.Foreground = resolved
                ? context.GetDrawable(outValue.ResourceId)
                : new Android.Graphics.Drawables.RippleDrawable(
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Argb(80, 255, 255, 255)),
                    null, null);

            nativeView.Clickable = true;

            // Without this, the ripple draws to the view's full rectangular bounds,
            // ignoring the rounded shape AcrylicView paints for the background - corners
            // of the ripple would visibly poke out past the button's rounded edge.
            var radiusPx = CornerRadius * context.Resources.DisplayMetrics.Density;
            nativeView.OutlineProvider = new RoundedOutlineProvider(radiusPx);
            nativeView.ClipToOutline = true;
        }
        catch (Exception ex)
        {
            // Best-effort native styling - a failure here should never take the app
            // down, it should just fall back to no ripple.
            System.Diagnostics.Debug.WriteLine($"CButton.ApplyAndroidRipple failed: {ex}");
        }
    }

    private class RoundedOutlineProvider : Android.Views.ViewOutlineProvider
    {
        private readonly float radiusPx;

        public RoundedOutlineProvider(float radiusPx) => this.radiusPx = radiusPx;

        public override void GetOutline(Android.Views.View view, Android.Graphics.Outline outline)
        {
            // This is invoked by Android's own rendering pipeline, which can happen
            // before the view's first layout pass (Width/Height still 0) -
            // Outline.SetRoundRect throws on a degenerate empty rect.
            if (view.Width <= 0 || view.Height <= 0)
                return;

            outline.SetRoundRect(0, 0, view.Width, view.Height, radiusPx);
        }
    }

    // RippleDrawable only animates in response to the View's Pressed state changing.
    // MAUI's TapGestureRecognizer owns the native touch listener, which appears to
    // swallow the touch stream before Android's own click machinery ever sets that
    // state - so it's driven manually here instead, from the gesture that's already
    // firing reliably (OnTapped).
    private async Task AnimateAndroidRippleAsync()
    {
        try
        {
            if (MainGrid.Handler?.PlatformView is not Android.Views.View nativeView)
                return;

            nativeView.Pressed = true;
            await Task.Delay(120);
            nativeView.Pressed = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CButton.AnimateAndroidRippleAsync failed: {ex}");
        }
    }
#endif

    private static void OnFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CButton button)
        {
            button.CurrentFontFamily = button.IconFontFamily;
        }
    }
}