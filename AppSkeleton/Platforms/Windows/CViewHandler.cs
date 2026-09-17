using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Point = Windows.Foundation.Point;

namespace AppSkeleton;

// WinUI's ScrollViewer marks pointer events Handled once it starts tracking a drag,
// which would normally stop them bubbling up before a plain event handler on an
// ancestor ever saw them. Registering with handledEventsToo:true bypasses that, so
// this still observes every pointer move regardless of what a descendant scrollable
// control already did with it - no need to fight it for ownership the way
// Android/iOS do, since we're not trying to prevent the descendant from scrolling.
public partial class CViewHandler : ViewHandler<ICView, Grid>
{
    private Point downPoint;
    private bool decided;
    private bool isHorizontal;

    protected override Grid CreatePlatformView()
    {
        var grid = new Grid();

        grid.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
        grid.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
        grid.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnPointerReleased), true);
        // The OS can reclaim the pointer mid-drag (e.g. an edge-swipe system gesture) -
        // without this, a drag left dangling here would never call CView.DragCancel,
        // leaving the carousel translated mid-drag forever. Deliberately NOT also
        // listening for PointerCaptureLost: that one bubbles up from *any* descendant
        // that captured and released the pointer for its own purposes (e.g. a
        // ScrollViewer finishing an ordinary touch manipulation), which routinely
        // happens mid-swipe and was cancelling perfectly good drags that had nothing
        // to do with losing the touch stream.
        grid.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(OnPointerCanceled), true);

        return grid;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        downPoint = e.GetCurrentPoint(PlatformView).Position;
        decided = false;
        isHorizontal = false;
    }

    // Fires on every move, not just once a threshold is crossed - once direction-lock
    // decides the drag is horizontal, each subsequent move reports the finger's total
    // offset so CView.DragUpdate can move the pages 1:1 with it in real time.
    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(PlatformView).Position;
        double dx = point.X - downPoint.X;
        double dy = point.Y - downPoint.Y;

        if (!decided)
        {
            // Waits for whichever axis is actually dominant to clear the slop, rather
            // than deciding off whichever axis happens to cross it first - see the
            // matching comment in the Android SwipeInterceptingViewGroup.
            if (Math.Abs(dx) > CView.TouchSlop && Math.Abs(dx) > Math.Abs(dy) * CView.DirectionLockRatio)
            {
                decided = true;
                isHorizontal = true;
            }
            else if (Math.Abs(dy) > CView.TouchSlop && Math.Abs(dy) > Math.Abs(dx) * CView.DirectionLockRatio)
            {
                decided = true;
                isHorizontal = false;
            }
        }

        if (isHorizontal)
        {
            (VirtualView as CView)?.DragUpdate(dx);
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (isHorizontal)
        {
            var point = e.GetCurrentPoint(PlatformView).Position;
            double dx = point.X - downPoint.X;
            (VirtualView as CView)?.DragEnd(dx);
        }

        decided = false;
        isHorizontal = false;
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (isHorizontal)
        {
            (VirtualView as CView)?.DragCancel();
        }

        decided = false;
        isHorizontal = false;
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
