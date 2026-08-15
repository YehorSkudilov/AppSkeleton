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
    internal Action<int> OnSwipe { get; set; }

    private readonly UIPanGestureRecognizer panRecognizer;

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
        if (recognizer.State != UIGestureRecognizerState.Ended)
        {
            return;
        }

        var translation = recognizer.TranslationInView(this);

        if (Math.Abs(translation.X) > Math.Abs(translation.Y) * CView.DirectionLockRatio &&
            Math.Abs(translation.X) > CView.SwipeThreshold)
        {
            OnSwipe?.Invoke(translation.X < 0 ? 1 : -1);
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
