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

    public string SelectedFontFamily
    {
        get => (string)GetValue(SelectedFontFamilyProperty);
        set => SetValue(SelectedFontFamilyProperty, value);
    }
    public ICommand LoadPageCommand { get; set; }

    public CNavBar()
    {
        InitializeComponent();
        LoadPageCommand = new Command<CNavItem>(SelectItem);
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

        if (propertyName == FontFamilyProperty.PropertyName)
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

        selectedNavItem.Action?.Invoke();
    }
}