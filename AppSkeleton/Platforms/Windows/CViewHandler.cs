using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Media;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Rect = Windows.Foundation.Rect;

namespace AppSkeleton;

// Swipe-to-navigate is Windows-only disabled (see CreatePlatformView) - a mouse/
// trackpad-driven horizontal drag over page content reads as an accidental gesture
// far more often than an intentional one on desktop, unlike the touch-first
// Android/iOS/MacCatalyst targets this shares CView with.
public partial class CViewHandler : ViewHandler<ICView, Grid>
{
    // Android's FrameLayout (CView's native container there) clips its children to its
    // own bounds by default, and so does iOS/MacCatalyst's UIView in practice here - but
    // a WinUI Grid doesn't clip at all unless told to. Without this, CarouselTo/DragUpdate
    // sliding the outgoing and incoming pages via TranslationX just renders them past the
    // row's edges instead of cutting them off there, which looked like the two pages
    // overlapping instead of one cleanly pushing the other off-screen.
    protected override Grid CreatePlatformView()
    {
        var grid = new Grid();

        grid.SizeChanged += (_, e) =>
        {
            grid.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height),
            };
        };

        return grid;
    }

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
