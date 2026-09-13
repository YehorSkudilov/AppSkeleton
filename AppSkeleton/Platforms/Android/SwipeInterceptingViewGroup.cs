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
    // dx is reported in device-independent units (already converted from raw pixels),
    // matching the unit system CView measures/animates its pages in.
    internal Action<double> OnDragUpdate { get; set; }
    internal Action<double> OnDragEnd { get; set; }
    internal Action OnDragCancel { get; set; }

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

        // Without GetDefaultSize, an Exactly spec (the norm for a Grid cell/`*` row,
        // e.g. CView's own row in CAppShell) was being ignored in favor of whatever
        // CrossPlatformMeasure naturally wanted for the current page's content -
        // shrinking CView (and every page inside it) down to its content's natural
        // height instead of filling the row it was actually given.
        double width = GetDefaultSize(widthMeasureSpec.GetMode(), context.FromPixels(SuggestedMinimumWidth), size.Width, w);
        double height = GetDefaultSize(heightMeasureSpec.GetMode(), context.FromPixels(SuggestedMinimumHeight), size.Height, h);

        int measuredWidth = (int)context.ToPixels(width);
        int measuredHeight = (int)context.ToPixels(height);
        SetMeasuredDimension(measuredWidth, measuredHeight);
    }

    private static double GetDefaultSize(MeasureSpecMode mode, double minSize, double desiredSize, double constraint)
    {
        if (mode == MeasureSpecMode.AtMost)
            return Math.Min(Math.Max(minSize, desiredSize), constraint);
        if (mode == MeasureSpecMode.Unspecified)
            return Math.Max(minSize, desiredSize);
        return constraint;
    }

    // Deliberately doesn't call base.OnLayout (matching BorderViewGroup) - FrameLayout's
    // own onLayout would immediately re-position the content child using ITS OWN
    // gravity/measured-size rules, clobbering the exact bounds CrossPlatformArrange just
    // assigned. That's what was actually shrinking every page inside CView vertically:
    // MainGrid's native view was measured to its natural/unmeasured size by native
    // FrameLayout layout, not the full row height MAUI's own arrange had just given it.
    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        var width = Context.FromPixels(right) - Context.FromPixels(left);
        var height = Context.FromPixels(bottom) - Context.FromPixels(top);
        CrossPlatformArrange?.Invoke(new Rect(0, 0, width, height));
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
            // Reported on every move once OnInterceptTouchEvent has decided this drag is
            // horizontal-dominant, not just once at Up - lets CView.DragUpdate move the
            // pages 1:1 with the finger in real time instead of only reacting once the
            // drag finishes.
            case MotionEventActions.Move:
                if (intercepting)
                {
                    double dx = Context.FromPixels(ev.GetX() - downX);
                    OnDragUpdate?.Invoke(dx);
                }
                break;

            case MotionEventActions.Up:
                if (intercepting)
                {
                    double dx = Context.FromPixels(ev.GetX() - downX);
                    OnDragEnd?.Invoke(dx);
                }
                decided = false;
                intercepting = false;
                break;

            case MotionEventActions.Cancel:
                if (intercepting)
                {
                    OnDragCancel?.Invoke();
                }
                decided = false;
                intercepting = false;
                break;
        }

        return true;
    }
}
