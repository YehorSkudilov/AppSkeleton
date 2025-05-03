
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using UraniumUI.Icons.FontAwesome;

namespace AppSkeleton;

public partial class CAppShell : ContentPage, INotifyPropertyChanged
{

    public ObservableCollection<NavIconItem> NavIconItems { get; set; }
    public event PropertyChangedEventHandler PropertyChanged;
    public string Username { get; }
    public int Coins
    {
        get => _coins;
        set
        {
            if (_coins != value)
            {
                _coins = value;
                OnPropertyChanged(nameof(Coins));
            }
        }
    }

    Dictionary<string, object> pages;
    bool sideNavOpen = false;
    double sideNavOpenWidth = 200;
    double sideNavCloseWidth = 0;
    private int _coins;
    private Image? lastSelected = null;
    AdManager adManager;
  
    bool profileMenuOpen = false;
    public CAppShell(ObservableCollection<NavIconItem> navIconItems, AppSettings appSettings)
	{
		InitializeComponent();

        NavIconItems = navIconItems;
        pages = new Dictionary<string, object>();

        //Temp Placeholders
        Username = "John";
        adManager = new AdManager();
        AppName.Text = appSettings.AppName;

        BindingContext = this;
    }

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    void SavePage(object page)
    {
        string name = page.GetType().Name;
        if (!pages.ContainsKey(name))
        {
            pages.Add(name, page);
        }
    }

    public void SetDefaultPage(int index)
    {
        var gridAtIndex = NavBarFlexLayout.Children[index] as Grid;
        var image = gridAtIndex?.FindByName<Image>("Image");

        FontImageSource fontImageSource = ((FontImageSource)image.Source);
        fontImageSource.Color = (Color)Application.Current.Resources["PrimaryDark"];

        lastSelected = image;
    }

    public void LoadPage(object page, Image image = null, string name = null)
    {
        if (image != null)
        {
            if (lastSelected != null)
            {
                FontImageSource lastSelectedFontImageSource = ((FontImageSource)lastSelected.Source);
                lastSelectedFontImageSource.Color = Colors.White;
            }
            FontImageSource fontImageSource = ((FontImageSource)image.Source);
            fontImageSource.Color = (Color)Application.Current.Resources["PrimaryDark"]; ;
            lastSelected = image;
        }
        SavePage(page);
        string className = page.GetType().Name;
        Content.Content = (pages[className] as ContentPage).Content;
    }


    private void SideNavClicked(object sender, EventArgs e)
    {
        if (!sideNavOpen)
        {
            sideNavOpen = true;
            CloseSideNavBar.IsVisible = true;
            SideNav.Animate("SideNavExpand", new Animation(v => SideNav.WidthRequest = v, SideNav.Width, sideNavOpenWidth), 16, 250, Easing.Linear);
        }
        else
        {
            sideNavOpen = false;
            CloseSideNavBar.IsVisible = false;
            SideNav.Animate("SideNavCollapse", new Animation(v => SideNav.WidthRequest = v, SideNav.Width, sideNavCloseWidth), 16, 250, Easing.Linear);
        }
    }    
    private void ProfileMenuClicked(object sender = null, EventArgs e = null)
    {
        if (!profileMenuOpen)
        {
            profileMenuOpen = true;
            CloseProfileMenu.IsVisible = true;

            ProfileMenu.AnchorX = 1;
            ProfileMenu.AnchorY = 0;
            ProfileMenu.IsVisible = true;
            ProfileMenu.Animate("profileMenuCollapse", new Animation(
                v => ProfileMenu.Scale = v,
                ProfileMenu.Scale, 1, 
                Easing.Linear
            ), 16, 250);

        }
        else
        {
            profileMenuOpen = false;
            CloseProfileMenu.IsVisible = false;

            ProfileMenu.AnchorX = 1;
            ProfileMenu.AnchorY = 0;
            var animation = new Animation(
                v => ProfileMenu.Scale = v,
                ProfileMenu.Scale, 0,
                Easing.Linear
            );

            animation.Commit(ProfileMenu, "profileMenuCollapse", 16, 250, null,
                (v, c) => {
                    ProfileMenu.IsVisible = false; // Hide AFTER animation
                });
        }
    }

    private void OnCreateRewardedAdClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
            adManager.LoadAd();
        }
    }


    private void AccountButtonClicked(object sender, EventArgs e)
    {
        LoadPage(new Account());
        ProfileMenuClicked();
    }
}

