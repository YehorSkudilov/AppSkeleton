using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace AppSkeleton;

public partial class CViewHandler : ViewHandler<ICView, SwipeInterceptingView>
{
    protected override SwipeInterceptingView CreatePlatformView()
    {
        return new SwipeInterceptingView
        {
            CrossPlatformMeasure = new Func<double, double, Size>(VirtualView.CrossPlatformMeasure),
            CrossPlatformArrange = new Func<Rect, Size>(VirtualView.CrossPlatformArrange),
            OnDragUpdate = dx => (VirtualView as CView)?.DragUpdate(dx),
            OnDragEnd = dx => (VirtualView as CView)?.DragEnd(dx),
            OnDragCancel = () => (VirtualView as CView)?.DragCancel(),
        };
    }

    private static void MapContent(CViewHandler handler, ICView view)
    {
        var nativeView = handler?.PlatformView;
        if (nativeView == null) return;

        Microsoft.Maui.Platform.ViewExtensions.ClearSubviews(nativeView);

        if (view.Content is IView content && view.Handler != null)
        {
            var frameworkElement = content.ToPlatform(view.Handler.MauiContext);
            nativeView.AddSubview(frameworkElement);
        }
    }
}
