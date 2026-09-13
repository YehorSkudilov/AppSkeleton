using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace AppSkeleton;

// A UIScrollView (backing CollectionView/ScrollView) owns its own UIPanGestureRecognizer,
// which by default blocks any sibling/ancestor recognizer from firing for the same
// touch. AlwaysRecognizeSimultaneouslyDelegate lets ours run alongside it instead of
// losing the arbitration outright, and the direction-lock in HandlePan only actually
// treats the gesture as a page swipe once it's clearly more horizontal than vertical.
public class SwipeInterceptingView : UIView
{
    internal Func<double, double, Size> CrossPlatformMeasure { get; set; }
    internal Func<Rect, Size> CrossPlatformArrange { get; set; }
    internal Action<double> OnDragUpdate { get; set; }
    internal Action<double> OnDragEnd { get; set; }
    internal Action OnDragCancel { get; set; }

    private readonly UIPanGestureRecognizer panRecognizer;

    // Direction-lock decision for the drag currently in progress, same idea as the
    // Android/Windows handlers' own decided/intercepting fields - UIPanGestureRecognizer
    // reports Changed events from the moment TouchSlop is crossed in any direction, so
    // this is what tells a horizontal page-swipe apart from an incidental vertical drag.
    private bool decided;
    private bool isHorizontal;

    public SwipeInterceptingView()
    {
        panRecognizer = new UIPanGestureRecognizer(HandlePan)
        {
            Delegate = new AlwaysRecognizeSimultaneouslyDelegate(),
        };
        AddGestureRecognizer(panRecognizer);
    }

    private void HandlePan(UIPanGestureRecognizer recognizer)
    {
        var translation = recognizer.TranslationInView(this);

        switch (recognizer.State)
        {
            case UIGestureRecognizerState.Began:
                decided = false;
                isHorizontal = false;
                break;

            // Fires on every move, not just once released - once direction-lock decides
            // the drag is horizontal, each subsequent Changed event reports the
            // finger's total offset so CView.DragUpdate can move the pages 1:1 with it.
            case UIGestureRecognizerState.Changed:
                if (!decided &&
                    (Math.Abs(translation.X) > CView.TouchSlop || Math.Abs(translation.Y) > CView.TouchSlop))
                {
                    decided = true;
                    isHorizontal = Math.Abs(translation.X) > Math.Abs(translation.Y) * CView.DirectionLockRatio;
                }

                if (isHorizontal)
                {
                    OnDragUpdate?.Invoke(translation.X);
                }
                break;

            case UIGestureRecognizerState.Ended:
                if (isHorizontal)
                {
                    OnDragEnd?.Invoke(translation.X);
                }
                decided = false;
                isHorizontal = false;
                break;

            // The system reclaiming the gesture (e.g. an edge-swipe system gesture, or
            // the recognizer failing outright) - there's no reliable release position
            // here, so this always bounces back rather than trying to commit.
            case UIGestureRecognizerState.Cancelled:
            case UIGestureRecognizerState.Failed:
                if (isHorizontal)
                {
                    OnDragCancel?.Invoke();
                }
                decided = false;
                isHorizontal = false;
                break;
        }
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        Rect rect = Bounds.ToRectangle();
        CrossPlatformMeasure?.Invoke(rect.Width, rect.Height);
        CrossPlatformArrange?.Invoke(rect);
    }

    public override CGSize SizeThatFits(CGSize size)
    {
        return CrossPlatformMeasure?.Invoke(size.Width, size.Height).ToCGSize() ?? size;
    }
}

public class AlwaysRecognizeSimultaneouslyDelegate : UIGestureRecognizerDelegate
{
    public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        => true;
}
