
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using UraniumUI.Icons.FontAwesome;

namespace AppSkeleton;

public partial class CAppShell : ContentPage, INotifyPropertyChanged
{
    Stack<LoadPageParams> pageHistory;
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

    private Color coinsColor;
    public Color CoinsColor
    {
        get => coinsColor;
        set
        {
            if (coinsColor != value)
            {
                coinsColor = value;
                OnPropertyChanged(nameof(CoinsColor));
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
        pageHistory = new Stack<LoadPageParams>();
        NavIconItems = navIconItems;
        pages = new Dictionary<string, object>();

        //Temp Placeholders
        Username = "John";
        adManager = new AdManager(AfterAd);
        AppName.Text = appSettings.AppName;

        BindingContext = this;
    }

    protected override bool OnBackButtonPressed()
    {
        if(!(pageHistory.Count > 1))
            return false;   

        pageHistory.Pop();
        LoadPage(pageHistory.Pop());

        return true;
    }

    public bool EnoughPoints(int points)
    {
        bool enough = Coins >= points;
        if (!enough)
        {
            NotEnoughPoints();
        }
        return enough;
    }

    public bool UsePoints(int pointsUsed)
    {
        if (Coins >= pointsUsed)
        {
            Coins -= pointsUsed;
            return true;
        }
        else
        {
            NotEnoughPoints();
            return false;
        }
    }

    void NotEnoughPoints()
    {
        ProfileMenuClicked();
        this.DisplayAlert("Not Enough Coins", "Click Get Coins to Get More", "Ok");
        NotEnoughPointsAnimation();
    }

    private async void NotEnoughPointsAnimation()
    {
        for (int i = 0; i < 3; i++)
        {
            CoinsColor = Colors.Red;
            await Task.Delay(500); // Shorter flash looks snappier
            CoinsColor = Colors.White;
            await Task.Delay(500); // Prevents immediate overwrite
        }
    }


    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    string SavePage(LoadPageParams lpp)
    {
        if (lpp.Name == null)
        {
            lpp.Name = lpp.Page.GetType().Name;
        }
        if (!pages.ContainsKey(lpp.Name))
        {
            pages.Add(lpp.Name, lpp.Page);
        }
        else if(lpp.Replace)
        {
            pages[lpp.Name] = lpp.Page;
        }
            return lpp.Name;
    }

    public void SetDefaultPage(int index)
    {
        var gridAtIndex = NavBarFlexLayout.Children[index] as Grid;
        var image = gridAtIndex?.FindByName<Image>("Image");
        NavIconItems[index].LoadPageCommand.Execute(image);
    }

    public void LoadPage(LoadPageParams lpp)
    {
        if (lpp.Image != null)
        {
            if (lastSelected != null)
            {
                FontImageSource lastSelectedFontImageSource = ((FontImageSource)lastSelected.Source);
                lastSelectedFontImageSource.Color = Colors.White;
            }
            FontImageSource fontImageSource = ((FontImageSource)lpp.Image.Source);
            fontImageSource.Color = (Color)Application.Current.Resources["PrimaryDark"]; ;
            lastSelected = lpp.Image;
        }

        string className = SavePage(lpp);
        Content.Content = (pages[className] as ContentView);
        if ((pageHistory.Count > 0) && (pageHistory.FirstOrDefault().Page.GetType() != lpp.Page.GetType()))
        {
            pageHistory.Push(lpp);
            Debug.WriteLine("added new" + pageHistory.FirstOrDefault().Page.GetType() + " " + lpp.Page.GetType());

        }

        if (!(pageHistory.Count > 0))
        {
            pageHistory.Push(lpp);
            Debug.WriteLine("added default");
        }

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
    public void ProfileMenuClicked(object sender = null, EventArgs e = null)
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

    void AfterAd()
    {
        Coins += 10;
        GetPointsButton.IsEnabled = true;
    }


    private void AccountButtonClicked(object sender, EventArgs e)
    {
        LoadPage(new LoadPageParams { Page = new Account() });
        ProfileMenuClicked();
    }
}

