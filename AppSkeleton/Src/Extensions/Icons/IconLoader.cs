using Microsoft.Maui.Hosting;

namespace AppSkeleton;

public static class IconLoader
{
    public static (string File, string Alias)[] Fonts => new[]
    {
        ("MaterialIcons-Regular.ttf", "RegularIcons"),
        ("MaterialIconsOutlined-Regular.otf", "OutlinedIcons")
    };

    public static IFontCollection AddCIcons(this IFontCollection fonts)
    {
        foreach (var font in Fonts)
        {
            fonts.AddFont(font.File, font.Alias);
        }

        return fonts;
    }
}