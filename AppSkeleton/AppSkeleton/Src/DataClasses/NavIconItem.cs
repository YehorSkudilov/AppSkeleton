

using System.Windows.Input;

namespace AppSkeleton
{
    public class NavIconItem
    {
        public ICommand? LoadPageCommand { get; set; }
        public LoadPageParams LoadPageParams { get; set; }
        public string? Glyph { get; set; }
        public Color? Color {get; set;}
    }

}
