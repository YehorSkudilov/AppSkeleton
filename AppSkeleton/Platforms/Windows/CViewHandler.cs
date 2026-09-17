using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace AppSkeleton;

// Swipe-to-navigate is Windows-only disabled (see CreatePlatformView) - a mouse/
// trackpad-driven horizontal drag over page content reads as an accidental gesture
// far more often than an intentional one on desktop, unlike the touch-first
// Android/iOS/MacCatalyst targets this shares CView with.
public partial class CViewHandler : ViewHandler<ICView, Grid>
{
    protected override Grid CreatePlatformView() => new Grid();

    private static void MapContent(CViewHandler handler, ICView view)
    {
        if (handler?.PlatformView == null) return;

        handler.PlatformView.Children.Clear();

        if (view.Content is IView content && view.Handler != null)
        {
            handler.PlatformView.Children.Add(ElementExtensions.ToPlatform(content, view.Handler.MauiContext));
        }
    }
}
