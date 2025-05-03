using System.Collections.ObjectModel;

namespace AppSkeleton;

public partial class CWindow : Window
{
    public CAppShell cAppShell;
    public CWindow(ObservableCollection<NavIconItem> navIconItems, AppSettings appSettings)
	{
		InitializeComponent();
		cAppShell = new CAppShell(navIconItems, appSettings);
        Page = cAppShell;

    }
}