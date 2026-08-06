

using System.Runtime.CompilerServices;

namespace AppSkeleton;

public partial class AcrylicView : ContentView, IAcrylicView
{
    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(AcrylicView), Colors.Transparent);

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(nameof(BorderThickness), typeof(Thickness), typeof(AcrylicView), new Thickness(1.0));

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
    nameof(CornerRadius),
    typeof(Thickness),
    typeof(AcrylicView), new Thickness(5.0));

    public static readonly BindableProperty EffectStyleProperty = BindableProperty.Create(
        nameof(EffectStyle),
        typeof(EffectStyle),
        typeof(AcrylicView), EffectStyle.Custom);

    public static readonly BindableProperty TintColorProperty = BindableProperty.Create(
        nameof(TintColor),
        typeof(Color),
        typeof(AcrylicView),
        Colors.Transparent);  // Safe default

    public AcrylicView()
    {

        // Platform check safely done at instance creation time
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            TintColor = Colors.LightGray;
    }

    public static readonly BindableProperty TintOpacityProperty = BindableProperty.Create(
        nameof(TintOpacity),
        typeof(double),
        typeof(AcrylicView), 0.0);

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public Thickness BorderThickness
    {
        get => (Thickness)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public Thickness CornerRadius
    {
        get => (Thickness)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        if (width <= 0 || height <= 0)
            return;

        base.OnSizeAllocated(width, height);
    }

    public EffectStyle EffectStyle
    {
        get => (EffectStyle)GetValue(EffectStyleProperty);
        set => SetValue(EffectStyleProperty, value);
    }

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public double TintOpacity
    {
        get => (double)GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // Previously gated behind "WidthRequest > 0 AND HeightRequest > 0" and only
        // watched WidthRequest/HeightRequest - meant setting e.g. MaximumHeightRequest
        // alone (CFilterModalPage does exactly this) never triggered a re-measure, so
        // the native view kept its old size no matter what the XAML/code changed it to.
        if (propertyName == WidthRequestProperty.PropertyName ||
            propertyName == HeightRequestProperty.PropertyName ||
            propertyName == MinimumWidthRequestProperty.PropertyName ||
            propertyName == MinimumHeightRequestProperty.PropertyName ||
            propertyName == MaximumWidthRequestProperty.PropertyName ||
            propertyName == MaximumHeightRequestProperty.PropertyName)
        {
            InvalidateMeasure();
        }
    }
}