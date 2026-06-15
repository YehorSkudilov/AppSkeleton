using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace AppSkeleton;
public class CNavItem : INotifyPropertyChanged
{
    public string Glyph { get; set; }
    public Color Color { get; set; }
    public ContentView Content { get; set; }
    public string PageName { get; set; }

    private string fontFamily;
    public string FontFamily
    {
        get => fontFamily;
        set
        {
            if (fontFamily == value) return;
            fontFamily = value;
            OnPropertyChanged();
        }
    }


    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value) return;
            isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}