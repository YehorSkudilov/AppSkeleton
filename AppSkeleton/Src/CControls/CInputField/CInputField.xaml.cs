using System.Windows.Input;

namespace AppSkeleton;

// One text field for the whole app: the box around the input, an optional leading icon and trailing content, and a
// show/hide toggle for passwords - instead of a Border+Entry pair (and the password Grid) repeated at every field.
//
// The input's own look (text colour, font, placeholder colour) is left to the app's implicit Entry style: those
// properties here only reach the Entry once they're set, so a field that doesn't set them looks like any other Entry.
// The box is styled through FieldBackgroundColor/FieldStroke/CornerRadius/FieldPadding, typically from an implicit
// CInputField style in the app.
public partial class CInputField : ContentView
{
    // ---------------- TEXT ----------------
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CInputField), string.Empty, BindingMode.TwoWay);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // ---------------- INPUT (forwarded to the Entry once set) ----------------
    public static readonly BindableProperty PlaceholderProperty = Forward<string?>(nameof(Placeholder), null, (e, v) => e.Placeholder = v);
    public static readonly BindableProperty PlaceholderColorProperty = Forward<Color?>(nameof(PlaceholderColor), null, (e, v) => e.PlaceholderColor = v);
    public static readonly BindableProperty TextColorProperty = Forward<Color?>(nameof(TextColor), null, (e, v) => e.TextColor = v);
    public static readonly BindableProperty FontSizeProperty = Forward(nameof(FontSize), 0d, (e, v) => e.FontSize = v);
    public static readonly BindableProperty FontFamilyProperty = Forward<string?>(nameof(FontFamily), null, (e, v) => e.FontFamily = v);
    public static readonly BindableProperty FontAttributesProperty = Forward(nameof(FontAttributes), FontAttributes.None, (e, v) => e.FontAttributes = v);
    public static readonly BindableProperty CharacterSpacingProperty = Forward(nameof(CharacterSpacing), 0d, (e, v) => e.CharacterSpacing = v);
    public static readonly BindableProperty HorizontalTextAlignmentProperty = Forward(nameof(HorizontalTextAlignment), TextAlignment.Start, (e, v) => e.HorizontalTextAlignment = v);
    public static readonly BindableProperty KeyboardProperty = Forward(nameof(Keyboard), Keyboard.Default, (e, v) => e.Keyboard = v);
    public static readonly BindableProperty MaxLengthProperty = Forward(nameof(MaxLength), int.MaxValue, (e, v) => e.MaxLength = v);
    public static readonly BindableProperty IsSpellCheckEnabledProperty = Forward(nameof(IsSpellCheckEnabled), true, (e, v) => e.IsSpellCheckEnabled = v);
    public static readonly BindableProperty IsTextPredictionEnabledProperty = Forward(nameof(IsTextPredictionEnabled), true, (e, v) => e.IsTextPredictionEnabled = v);
    public static readonly BindableProperty ReturnTypeProperty = Forward(nameof(ReturnType), ReturnType.Default, (e, v) => e.ReturnType = v);
    public static readonly BindableProperty ReturnCommandProperty = Forward<ICommand?>(nameof(ReturnCommand), null, (e, v) => e.ReturnCommand = v);
    public static readonly BindableProperty ReturnCommandParameterProperty = Forward<object?>(nameof(ReturnCommandParameter), null, (e, v) => e.ReturnCommandParameter = v);
    public static readonly BindableProperty ClearButtonVisibilityProperty = Forward(nameof(ClearButtonVisibility), ClearButtonVisibility.Never, (e, v) => e.ClearButtonVisibility = v);
    public static readonly BindableProperty IsReadOnlyProperty = Forward(nameof(IsReadOnly), false, (e, v) => e.IsReadOnly = v);

    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public Color? PlaceholderColor { get => (Color?)GetValue(PlaceholderColorProperty); set => SetValue(PlaceholderColorProperty, value); }
    public Color? TextColor { get => (Color?)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public string? FontFamily { get => (string?)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public FontAttributes FontAttributes { get => (FontAttributes)GetValue(FontAttributesProperty); set => SetValue(FontAttributesProperty, value); }
    public double CharacterSpacing { get => (double)GetValue(CharacterSpacingProperty); set => SetValue(CharacterSpacingProperty, value); }
    public TextAlignment HorizontalTextAlignment { get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty); set => SetValue(HorizontalTextAlignmentProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
    public bool IsSpellCheckEnabled { get => (bool)GetValue(IsSpellCheckEnabledProperty); set => SetValue(IsSpellCheckEnabledProperty, value); }
    public bool IsTextPredictionEnabled { get => (bool)GetValue(IsTextPredictionEnabledProperty); set => SetValue(IsTextPredictionEnabledProperty, value); }
    public ReturnType ReturnType { get => (ReturnType)GetValue(ReturnTypeProperty); set => SetValue(ReturnTypeProperty, value); }
    public ICommand? ReturnCommand { get => (ICommand?)GetValue(ReturnCommandProperty); set => SetValue(ReturnCommandProperty, value); }
    public object? ReturnCommandParameter { get => GetValue(ReturnCommandParameterProperty); set => SetValue(ReturnCommandParameterProperty, value); }
    public ClearButtonVisibility ClearButtonVisibility { get => (ClearButtonVisibility)GetValue(ClearButtonVisibilityProperty); set => SetValue(ClearButtonVisibilityProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    // ---------------- PASSWORD ----------------
    // Hidden text with a show/hide toggle; the toggle only changes what's shown, not this property.
    public static readonly BindableProperty IsPasswordProperty =
        BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(CInputField), false,
            propertyChanged: (b, _, v) => ((CInputField)b).ApplyPassword((bool)v));

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    // ---------------- LEADING ICON / TRAILING CONTENT ----------------
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(ImageSource), typeof(CInputField),
            propertyChanged: (b, _, v) => ((CInputField)b).LeadingIcon.IsVisible = v != null);

    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(double), typeof(CInputField), 18d);

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    // After the input, e.g. a busy spinner. Keeps the page's BindingContext.
    public static readonly BindableProperty TrailingContentProperty =
        BindableProperty.Create(nameof(TrailingContent), typeof(View), typeof(CInputField),
            propertyChanged: (b, _, v) =>
            {
                var field = (CInputField)b;
                field.Trailing.Content = (View?)v;
                field.Trailing.IsVisible = v != null;
            });

    public View? TrailingContent
    {
        get => (View?)GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    // ---------------- BOX ----------------
    public static readonly BindableProperty FieldBackgroundColorProperty =
        BindableProperty.Create(nameof(FieldBackgroundColor), typeof(Color), typeof(CInputField), Color.FromArgb("#1AFFFFFF"));

    public Color FieldBackgroundColor
    {
        get => (Color)GetValue(FieldBackgroundColorProperty);
        set => SetValue(FieldBackgroundColorProperty, value);
    }

    public static readonly BindableProperty FieldStrokeProperty =
        BindableProperty.Create(nameof(FieldStroke), typeof(Color), typeof(CInputField), Colors.Transparent);

    public Color FieldStroke
    {
        get => (Color)GetValue(FieldStrokeProperty);
        set => SetValue(FieldStrokeProperty, value);
    }

    public static readonly BindableProperty FieldStrokeThicknessProperty =
        BindableProperty.Create(nameof(FieldStrokeThickness), typeof(double), typeof(CInputField), 0d);

    public double FieldStrokeThickness
    {
        get => (double)GetValue(FieldStrokeThicknessProperty);
        set => SetValue(FieldStrokeThicknessProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(CornerRadius), typeof(CInputField), new CornerRadius(10));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly BindableProperty FieldPaddingProperty =
        BindableProperty.Create(nameof(FieldPadding), typeof(Thickness), typeof(CInputField), new Thickness(0, 0));

    public Thickness FieldPadding
    {
        get => (Thickness)GetValue(FieldPaddingProperty);
        set => SetValue(FieldPaddingProperty, value);
    }

    // ---------------- EVENTS ----------------
    public event EventHandler? Completed;

    public event EventHandler<TextChangedEventArgs>? TextChanged;

    public CInputField()
    {
        InitializeComponent();
    }

    // A property that only reaches the Entry once it's set, so the app's implicit Entry style applies otherwise.
    static BindableProperty Forward<T>(string name, T defaultValue, Action<Entry, T> apply) =>
        BindableProperty.Create(name, typeof(T), typeof(CInputField), defaultValue,
            propertyChanged: (b, _, v) => apply(((CInputField)b).Input, (T)v!));

    void ApplyPassword(bool isPassword)
    {
        Input.IsPassword = isPassword;
        VisibilityToggle.IsVisible = isPassword;
        VisibilityToggle.Icon = "visibility";
    }

    void OnToggleVisibility(object? sender, EventArgs e)
    {
        Input.IsPassword = !Input.IsPassword;
        VisibilityToggle.Icon = Input.IsPassword ? "visibility" : "visibility_off";
    }

    void OnCompleted(object? sender, EventArgs e) => Completed?.Invoke(this, e);

    void OnTextChanged(object? sender, TextChangedEventArgs e) => TextChanged?.Invoke(this, e);

    // Shadows VisualElement.Focus/Unfocus (neither is virtual), as CPasswordEntry does: callers holding a CInputField
    // focus the input itself rather than this ContentView, which can't take focus.
    public new bool Focus() => Input.Focus();

    public new void Unfocus() => Input.Unfocus();
}
