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

        return grid;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        downPoint = e.GetCurrentPoint(PlatformView).Position;
        decided = false;
        isHorizontal = false;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (decided) return;

        var point = e.GetCurrentPoint(PlatformView).Position;
        double dx = point.X - downPoint.X;
        double dy = point.Y - downPoint.Y;

        if (Math.Abs(dx) > CView.TouchSlop || Math.Abs(dy) > CView.TouchSlop)
        {
            decided = true;
            isHorizontal = Math.Abs(dx) > Math.Abs(dy) * CView.DirectionLockRatio;
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (isHorizontal)
        {
            var point = e.GetCurrentPoint(PlatformView).Position;
            double dx = point.X - downPoint.X;

            if (Math.Abs(dx) > CView.SwipeThreshold)
            {
                (VirtualView as CView)?.Swipe(dx < 0 ? 1 : -1);
            }
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
