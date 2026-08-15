using Android.Content;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using Rect = Microsoft.Maui.Graphics.Rect;
using View = Android.Views.View;

namespace AppSkeleton;

// Hosts CView's content the same way BorderViewGroup hosts AcrylicView's, but also
// steals the touch stream from a descendant scrollable control (RecyclerView-backed
// CollectionView, ScrollView, ...) once a drag turns out to be horizontal-dominant.
// A plain SwipeGestureRecognizer can't do this: once a scrollable child's own
// onTouchEvent returns true for ACTION_DOWN, it owns the whole gesture stream and a
// recognizer on an ancestor never sees the MOVE/UP events. Overriding
// OnInterceptTouchEvent here is the one thing that lets an ancestor reclaim it -
// exactly how ViewPager/RecyclerView-swipe implementations do this on Android.
public class SwipeInterceptingViewGroup(Context context) : FrameLayout(context)
{
    internal Func<double, double, Size> CrossPlatformMeasure { get; set; }
    internal Func<Rect, Size> CrossPlatformArrange { get; set; }
    internal Action<int> OnSwipe { get; set; }

    private readonly int touchSlopPx = ViewConfiguration.Get(context).ScaledTouchSlop;

    private float downX;
    private float downY;
    private bool decided;
    private bool intercepting;

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        Context context = Context;
        if (context == null)
        {
            return;
        }
        if (CrossPlatformMeasure == null)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            return;
        }
        double w = widthMeasureSpec.ToDouble(context);
        double h = heightMeasureSpec.ToDouble(context);
        Size size = CrossPlatformMeasure(w, h);

        int measuredWidth = (int)context.ToPixels(size.Width);
        int measuredHeight = (int)context.ToPixels(size.Height);
        SetMeasuredDimension(measuredWidth, measuredHeight);
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        var width = Context.FromPixels(right) - Context.FromPixels(left);
        var height = Context.FromPixels(bottom) - Context.FromPixels(top);
        CrossPlatformArrange?.Invoke(new Rect(0, 0, width, height));
        base.OnLayout(changed, left, top, right, bottom);
    }

    public override bool OnInterceptTouchEvent(MotionEvent? ev)
    {
        if (ev == null) return intercepting;

        switch (ev.Action)
        {
            case MotionEventActions.Down:
                downX = ev.GetX();
                downY = ev.GetY();
                decided = false;
                intercepting = false;
                break;

            case MotionEventActions.Move:
                if (!decided)
                {
                    float dx = ev.GetX() - downX;
                    float dy = ev.GetY() - downY;
                    if (Math.Abs(dx) > touchSlopPx || Math.Abs(dy) > touchSlopPx)
                    {
                        decided = true;
                        intercepting = Math.Abs(dx) > Math.Abs(dy) * CView.DirectionLockRatio;
                    }
                }
                break;

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                decided = false;
                intercepting = false;
                break;
        }

        return intercepting;
    }

    public override bool OnTouchEvent(MotionEvent? ev)
    {
        if (ev == null) return true;

        switch (ev.Action)
        {
            case MotionEventActions.Up:
                if (intercepting)
                {
                    float dx = ev.GetX() - downX;
                    double thresholdPx = Context.ToPixels(CView.SwipeThreshold);
                    if (Math.Abs(dx) > thresholdPx)
                    {
                        OnSwipe?.Invoke(dx < 0 ? 1 : -1);
                    }
                }
                decided = false;
                intercepting = false;
                break;

            case MotionEventActions.Cancel:
                decided = false;
                intercepting = false;
                break;
        }

        return true;
    }
}
