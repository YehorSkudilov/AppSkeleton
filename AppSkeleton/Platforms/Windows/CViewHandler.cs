using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Media;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using MauiRect = Microsoft.Maui.Graphics.Rect;
using WinRect = Windows.Foundation.Rect;
using WinSize = Windows.Foundation.Size;

namespace AppSkeleton;

// Swipe-to-navigate is Windows-only disabled (see CreatePlatformView) - a mouse/
// trackpad-driven horizontal drag over page content reads as an accidental gesture
// far more often than an intentional one on desktop, unlike the touch-first
// Android/iOS/MacCatalyst targets this shares CView with.
public partial class CViewHandler : ViewHandler<ICView, Grid>
{
    protected override Grid CreatePlatformView()
    {
        return new CViewContainer
        {
            CrossPlatformMeasure = (w, h) => VirtualView?.CrossPlatformMeasure(w, h) ?? Size.Zero,
            CrossPlatformArrange = rect => VirtualView?.CrossPlatformArrange(rect) ?? Size.Zero,
        };
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

// Android's FrameLayout and iOS/MacCatalyst's UIView containers for CView both feed
// their native measure/layout pass back into MAUI's cross-platform layout via
// CrossPlatformMeasure/CrossPlatformArrange (see SwipeInterceptingViewGroup/View and
// their CViewHandlers) - a plain Grid here never did that, so CView's own Content (its
// internal MainGrid, see CView.xaml.cs) never got arranged past whatever its very
// first pass happened to produce. MainGrid.Width/Height effectively never updated
// again after that on Windows, so CarouselTo/DragUpdate kept falling back to their
// hardcoded 300-unit page width (see CarouselTo's `if (width <= 0) width = 300;`),
// which only looked right when the window was coincidentally close to 300 units wide
// and visibly overlapped the wider it got.
internal class CViewContainer : Grid
{
    internal Func<double, double, Size> CrossPlatformMeasure { get; set; }
    internal Func<MauiRect, Size> CrossPlatformArrange { get; set; }

    protected override WinSize MeasureOverride(WinSize availableSize)
    {
        if (CrossPlatformMeasure == null)
            return base.MeasureOverride(availableSize);

        var size = CrossPlatformMeasure(availableSize.Width, availableSize.Height);
        return new WinSize(size.Width, size.Height);
    }

    // Deliberately doesn't call base.ArrangeOverride - Grid's own default arrange would
    // re-position the content child using ITS OWN cell-fill rules instead of the exact
    // bounds CrossPlatformArrange (MAUI's own layout math for CView's Content) just
    // assigned, the same reasoning as the Android SwipeInterceptingViewGroup not
    // calling base.OnLayout.
    //
    // Also clips to the freshly-arranged bounds here, same reasoning as before: a
    // WinUI Grid doesn't clip its children by default the way Android's FrameLayout
    // does, so the sliding pages would otherwise render past the row's edges instead
    // of being cut off there.
    protected override WinSize ArrangeOverride(WinSize finalSize)
    {
        CrossPlatformArrange?.Invoke(new MauiRect(0, 0, finalSize.Width, finalSize.Height));

        Clip = new RectangleGeometry
        {
            Rect = new WinRect(0, 0, finalSize.Width, finalSize.Height),
        };

        return finalSize;
    }
}
