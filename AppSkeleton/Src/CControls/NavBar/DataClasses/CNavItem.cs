using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace AppSkeleton;
public class CNavItem : INotifyPropertyChanged
{
    public string Glyph { get; set; }
    public Color Color { get; set; }
    public PageBase Page { get; set; }
    public string PageName { get; set; }

    // Optional sizing, e.g. for one larger, prominent item such as a centre button. The defaults are the bar's
    // standard look, so items that don't set them are unchanged.

    /// <summary>The icon's width and height.</summary>
    public double IconSize { get; set; } = 24;

    /// <summary>The width of the item's tap area.</summary>
    public double ItemWidth { get; set; } = 64;

    /// <summary>A filled circle behind the icon; none when null.</summary>
    public Color IconBackground { get; set; }

    /// <summary>The circle's diameter; it's only drawn with <see cref="IconBackground"/> set.</summary>
    public double IconBackgroundSize { get; set; }

    /// <summary>Whether the name shows under the icon; a large round button often goes without.</summary>
    public bool ShowLabel { get; set; } = true;

    public bool HasIconBackground => (IconBackground != null || SelectedIconBackground != null) && IconBackgroundSize > 0;

    // Optional looks for the selected item, so it stands out beyond the filled/outlined font swap (which looks the
    // same for many icons). Each falls back to the normal one when not set, so items without them are unchanged.

    /// <summary>The icon while selected, e.g. a filled variant.</summary>
    public string SelectedGlyph { get; set; }

    /// <summary>The icon's colour while selected.</summary>
    public Color SelectedColor { get; set; }

    /// <summary>The circle behind the icon while selected (with <see cref="IconBackgroundSize"/>).</summary>
    public Color SelectedIconBackground { get; set; }

    // What the bar shows: the selected look when selected and set, otherwise the normal one.
    public string CurrentGlyph => IsSelected && !string.IsNullOrEmpty(SelectedGlyph) ? SelectedGlyph : Glyph;
    public Color CurrentColor => IsSelected && SelectedColor != null ? SelectedColor : Color;
    public Color CurrentIconBackground => IsSelected && SelectedIconBackground != null ? SelectedIconBackground : IconBackground;

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
            OnPropertyChanged(nameof(CurrentGlyph));
            OnPropertyChanged(nameof(CurrentColor));
            OnPropertyChanged(nameof(CurrentIconBackground));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}