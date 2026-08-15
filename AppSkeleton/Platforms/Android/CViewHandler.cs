using Android.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace AppSkeleton;

public partial class CViewHandler : ViewHandler<ICView, FrameLayout>
{
    private SwipeInterceptingViewGroup container;

    protected override FrameLayout CreatePlatformView()
    {
        container = new SwipeInterceptingViewGroup(Context)
        {
            CrossPlatformMeasure = (w, h) => VirtualView?.CrossPlatformMeasure(w, h) ?? Size.Zero,
            CrossPlatformArrange = (rect) => VirtualView?.CrossPlatformArrange(rect) ?? Size.Zero,
            OnSwipe = direction => (VirtualView as CView)?.Swipe(direction),
        };

        return container;
    }

    private static void MapContent(CViewHandler handler, ICView view)
    {
        if (handler?.container == null) return;

        handler.container.RemoveAllViews();

        if (view.Content is IView content && view.Handler?.MauiContext != null)
        {
            var platformView = content.ToPlatform(view.Handler.MauiContext);
            handler.container.AddView(platformView);
        }
    }

    protected override void DisconnectHandler(FrameLayout platformView)
    {
        container = null;
        base.DisconnectHandler(platformView);
    }
}
