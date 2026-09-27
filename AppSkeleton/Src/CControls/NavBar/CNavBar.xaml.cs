using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace AppSkeleton;

public partial class CNavBar : ContentView
{
    public static readonly BindableProperty CNavIconItemsProperty =
        BindableProperty.Create(
            nameof(CNavIconItems),
            typeof(ObservableCollection<CNavItem>),
            typeof(CNavBar),
            new ObservableCollection<CNavItem>()
        );

    public ObservableCollection<CNavItem> CNavIconItems
    {
        get => (ObservableCollection<CNavItem>)GetValue(CNavIconItemsProperty);
        set
        {
            SetValue(CNavIconItemsProperty, value);
            ApplyDefaultFont(value);
        }
    }

    public static readonly BindableProperty FontFamilyProperty =
    BindableProperty.Create(
        nameof(FontFamily),
        typeof(string),
        typeof(CNavBar),
        default(string));

    public static readonly BindableProperty SelectedFontFamilyProperty =
        BindableProperty.Create(
            nameof(SelectedFontFamily),
            typeof(string),
            typeof(CNavBar),
            default(string));

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public static readonly BindableProperty IsNavigatingProperty =
        BindableProperty.Create(
            nameof(IsNavigating),
            typeof(bool),
            typeof(CNavBar),
            default(bool));

    public bool IsNavigating
    {
        get => (bool)GetValue(IsNavigatingProperty);
        set => SetValue(IsNavigatingProperty, value);
    }

    public string SelectedFontFamily
    {
        get => (string)GetValue(SelectedFontFamilyProperty);
        set => SetValue(SelectedFontFamilyProperty, value);
    }

    // Appearance of the bar's acrylic background and labels. Defaults are the original frosted-white look.
    public static readonly BindableProperty TintColorProperty =
        BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(CNavBar), Color.FromArgb("#20ffffff"));

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public static readonly BindableProperty TintOpacityProperty =
        BindableProperty.Create(nameof(TintOpacity), typeof(double), typeof(CNavBar), .5);

    public double TintOpacity
    {
        get => (double)GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty =
        BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(CNavBar), Color.FromArgb("#40ffffff"));

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(Thickness), typeof(CNavBar), new Thickness(20));

    public Thickness CornerRadius
    {
        get => (Thickness)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly BindableProperty LabelColorProperty =
        BindableProperty.Create(nameof(LabelColor), typeof(Color), typeof(CNavBar), Colors.White);

    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }


    public static readonly BindableProperty LoadPageCommandProperty =
        BindableProperty.Create(
            nameof(LoadPageCommand),
            typeof(Command<CNavItem>),
            typeof(CNavBar),
            default(Command<CNavItem>));

    public Command<CNavItem> LoadPageCommand
    {
        get => (Command<CNavItem>)GetValue(LoadPageCommandProperty);
        set => SetValue(LoadPageCommandProperty, value);
    }

    public Command<CNavItem> SelectPageCommand { get; set; }

    public CNavBar()
    {
        InitializeComponent();
        SelectPageCommand = new Command<CNavItem>(SelectItem);
        MainGrid.BindingContext = this;
    }

    private void ApplyDefaultFont(ObservableCollection<CNavItem> items)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            ApplyFont(item);
        }

        items.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (CNavItem item in e.NewItems)
                {
                    ApplyFont(item);
                }
            }
        };
    }

    private void ApplyFont(CNavItem item)
    {
        if (string.IsNullOrWhiteSpace(item.FontFamily))
            item.FontFamily = FontFamily;
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == FontFamilyProperty.PropertyName || propertyName == CNavIconItemsProperty.PropertyName)
        {
            ApplyDefaultFont(CNavIconItems);
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        ApplyDefaultFont(CNavIconItems);
    }


    public void SelectItem(CNavItem selectedNavItem)
    {
        // No longer blocked while IsNavigating - CView.SetContent now retargets an
        // already-running carousel step-loop instead of requiring the tap to wait
        // for the whole (possibly multi-tab) transition to finish first.
        foreach (var navItem in CNavIconItems)
        {
            if(navItem != selectedNavItem)
            {
                navItem.IsSelected = false;
                navItem.FontFamily = FontFamily;
            }
        }

        if (selectedNavItem == null)
            return;

        selectedNavItem.IsSelected = true;
        selectedNavItem.FontFamily = SelectedFontFamily;
        LoadPageCommand.Execute(selectedNavItem);
    }
}