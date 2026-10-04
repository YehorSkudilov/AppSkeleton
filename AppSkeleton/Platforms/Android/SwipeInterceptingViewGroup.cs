using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Platform;
using Rect = Microsoft.Maui.Graphics.Rect;
using View = Android.Views.View;

namespace AppSkeleton;

// Hosts CView's content the same way BorderViewGroup hosts AcrylicView's, but also
// steals the touch stream from a descendant scrollable control (RecyclerView-backed
// CollectionView, ScrollView, ...) once a drag turns out to be horizontal-dominant, or
// vertical-dominant-and-downward while that descendant is already scrolled to its top
// (a pull-to-refresh gesture). A plain SwipeGestureRecognizer can't do this: once a
// scrollable child's own onTouchEvent returns true for ACTION_DOWN, it owns the whole
// gesture stream and a recognizer on an ancestor never sees the MOVE/UP events.
// Overriding OnInterceptTouchEvent here is the one thing that lets an ancestor reclaim
// it - exactly how ViewPager/RecyclerView-swipe implementations do this on Android.
public class SwipeInterceptingViewGroup(Context context) : FrameLayout(context)
{
    internal Func<double, double, Size> CrossPlatformMeasure { get; set; }
    internal Func<Rect, Size> CrossPlatformArrange { get; set; }
    // dx/dy are reported in device-independent units (already converted from raw
    // pixels), matching the unit system CView measures/animates its pages in.
    internal Action<double> OnDragUpdate { get; set; }
    internal Action<double> OnDragEnd { get; set; }
    internal Action OnDragCancel { get; set; }
    internal Action<double> OnPullUpdate { get; set; }
    internal Action<double> OnPullEnd { get; set; }
    internal Action OnPullCancel { get; set; }

    private readonly int touchSlopPx = ViewConfiguration.Get(context).ScaledTouchSlop;

    private enum GestureKind { None, Horizontal, Pull }

    private float downX;
    private float downY;
    private bool decided;
    private GestureKind gestureKind;

    // Whether a downward drag starting now would count as pull-to-refresh - computed
    // once at ACTION_DOWN (see IsAtTop) and held for the rest of this gesture, rather
    // than re-checked on every move, so a list that scrolls itself away from the top
    // mid-drag can't retroactively cancel a pull that's already committed to.
    private bool pullEligibleAtDown;

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

    // Shared by OnInterceptTouchEvent and OnTouchEvent's own Move handling (see the
    // comment on OnTouchEvent below for why both need it) - waits for whichever axis is
    // actually dominant to clear the slop, rather than deciding off whichever axis
    // happens to cross it first, since a real horizontal swipe rarely moves in a
    // perfectly straight line and locking the decision in off an incidental few pixels
    // of vertical jitter (before the horizontal motion itself had even cleared slop yet)
    // intermittently swallowed otherwise-clean swipes as "not horizontal" for the whole
    // gesture. A vertical-dominant drag only ever claims the gesture (GestureKind.Pull)
    // when it's downward and pullEligibleAtDown - otherwise it's left GestureKind.None
    // so the descendant scrollable (a CollectionView scrolling normally) still gets it,
    // same as before this gained a third kind.
    private void DecideDirectionIfNeeded(float dx, float dy)
    {
        if (decided)
            return;

        if (Math.Abs(dx) > touchSlopPx && Math.Abs(dx) > Math.Abs(dy) * CView.DirectionLockRatio)
        {
            decided = true;
            gestureKind = GestureKind.Horizontal;
        }
        else if (dy > touchSlopPx && dy > Math.Abs(dx) * CView.DirectionLockRatio && pullEligibleAtDown)
        {
            decided = true;
            gestureKind = GestureKind.Pull;
        }
        else if (Math.Abs(dy) > touchSlopPx && Math.Abs(dy) > Math.Abs(dx) * CView.DirectionLockRatio)
        {
            decided = true;
            gestureKind = GestureKind.None;
        }
    }

    // True when every scrollable descendant (RecyclerView-backed CollectionView,
    // ScrollView, ...) is already at the top of its content, or there's no scrollable
    // descendant at all (a page like HomePage with nothing to scroll - the whole page
    // is fair game to pull from). False the moment any one of them could still scroll
    // upward, since a downward drag there means "reveal earlier content", not "pull to
    // refresh".
    private static bool IsAtTop(View view)
    {
        switch (view)
        {
            case RecyclerView or Android.Widget.ScrollView or NestedScrollView or Android.Widget.ListView or Android.Widget.GridView:
                return !view.CanScrollVertically(-1);

            case ViewGroup group:
                for (int i = 0; i < group.ChildCount; i++)
                {
                    var child = group.GetChildAt(i);
                    // A tab page CView has parked off to the side (see CView.Park) isn't the one being pulled.
                    if (child == null || (group.Width > 0 && Math.Abs(child.TranslationX) >= group.Width))
                        continue;
                    if (!IsAtTop(child))
                        return false;
                }
                return true;

            default:
                return true;
        }
    }

    public override bool OnInterceptTouchEvent(MotionEvent? ev)
    {
        if (ev == null) return gestureKind != GestureKind.None;

        switch (ev.Action)
        {
            case MotionEventActions.Down:
                downX = ev.GetX();
                downY = ev.GetY();
                decided = false;
                gestureKind = GestureKind.None;
                pullEligibleAtDown = IsAtTop(this);
                break;

            case MotionEventActions.Move:
                DecideDirectionIfNeeded(ev.GetX() - downX, ev.GetY() - downY);
                break;

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                decided = false;
                gestureKind = GestureKind.None;
                break;
        }

        return gestureKind != GestureKind.None;
    }

    public override bool OnTouchEvent(MotionEvent? ev)
    {
        if (ev == null) return true;

        switch (ev.Action)
        {
            // Reported on every move once decided/gestureKind is set, not just once at
            // Up - lets CView.DragUpdate/PullUpdate move the pages (or the pull
            // indicator) 1:1 with the finger in real time instead of only reacting once
            // the drag finishes.
            //
            // Also re-runs the direction-lock decision itself here, not just in
            // OnInterceptTouchEvent above: Android only re-invokes onInterceptTouchEvent
            // on Move/Up if some child actually consumed the initial ACTION_DOWN (i.e.
            // ViewGroup's own mFirstTouchTarget ends up non-null) - a plain Label (a
            // non-clickable TextView, which is most of what these pages are built from)
            // never consumes it, so Android instead routes the whole gesture straight
            // to this method from the start, WITHOUT ever calling
            // OnInterceptTouchEvent again. Without this, decided/gestureKind stayed
            // stuck at None for the whole gesture and a swipe starting over any such
            // element silently never registered. Deciding is idempotent (see
            // DecideDirectionIfNeeded) so this is a no-op on the normal path where
            // OnInterceptTouchEvent already decided it.
            case MotionEventActions.Move:
                DecideDirectionIfNeeded(ev.GetX() - downX, ev.GetY() - downY);

                if (gestureKind == GestureKind.Horizontal)
                {
                    double dx = Context.FromPixels(ev.GetX() - downX);
                    OnDragUpdate?.Invoke(dx);
                }
                else if (gestureKind == GestureKind.Pull)
                {
                    double dy = Context.FromPixels(ev.GetY() - downY);
                    OnPullUpdate?.Invoke(dy);
                }
                break;

            case MotionEventActions.Up:
                if (gestureKind == GestureKind.Horizontal)
                {
                    double dx = Context.FromPixels(ev.GetX() - downX);
                    OnDragEnd?.Invoke(dx);
                }
                else if (gestureKind == GestureKind.Pull)
                {
                    double dy = Context.FromPixels(ev.GetY() - downY);
                    OnPullEnd?.Invoke(dy);
                }
                decided = false;
                gestureKind = GestureKind.None;
                break;

            case MotionEventActions.Cancel:
                if (gestureKind == GestureKind.Horizontal)
                {
                    OnDragCancel?.Invoke();
                }
                else if (gestureKind == GestureKind.Pull)
                {
                    OnPullCancel?.Invoke();
                }
                decided = false;
                gestureKind = GestureKind.None;
                break;
        }

        return true;
    }
}
