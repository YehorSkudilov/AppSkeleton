using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using MauiRect = Microsoft.Maui.Graphics.Rect;
using WinPoint = Windows.Foundation.Point;
using WinSlider = Microsoft.UI.Xaml.Controls.Slider;
using WinRect = Windows.Foundation.Rect;
using WinSize = Windows.Foundation.Size;

namespace AppSkeleton;

// Swipe-to-navigate on Windows follows the pointer the same way it does a finger on
// Android/iOS/MacCatalyst - a mouse drag with the left button held, touch or pen (see
// CViewContainer's pointer handlers) - and a two-finger sideways swipe on a trackpad,
// the same way it scrolls a page up and down (see OnPointerWheelChanged). Only a clearly horizontal drag counts (past
// TouchSlop, and DirectionLockRatio times more sideways than up/down), so clicks and
// vertical scrolling stay as they were.
public partial class CViewHandler : ViewHandler<ICView, Grid>
{
    protected override Grid CreatePlatformView()
    {
        return new CViewContainer
        {
            CrossPlatformMeasure = (w, h) => VirtualView?.CrossPlatformMeasure(w, h) ?? Size.Zero,
            CrossPlatformArrange = rect => VirtualView?.CrossPlatformArrange(rect) ?? Size.Zero,
            Owner = () => VirtualView as CView,
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
    internal Func<CView> Owner { get; set; }

    // ----- pointer-driven drag state -----
    // The pointer being watched since it went down (null when none) - a drag only ever
    // follows the one pointer that started it.
    private uint? trackedPointer;
    private WinPoint dragStart;
    // Past the direction lock: the pointer is captured and every move drives DragUpdate.
    private bool dragging;

    // ----- trackpad (horizontal wheel) drag state -----
    // A two-finger sideways swipe on a trackpad (or a tilt wheel) arrives as a stream of
    // horizontal wheel deltas, with no press or release around it - summed into a drag
    // offset like a finger's, which ends once the deltas stop (WheelIdleMs, long enough
    // to cover the gap before the trackpad's own inertia deltas start).
    private const int WheelIdleMs = 160;
    private double wheelDx;
    private bool wheelDragging;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer wheelTimer;

    public CViewContainer()
    {
        // Without a background, empty areas of the Grid aren't hit-tested at all.
        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        // handledEventsToo: pages are full of ScrollViewers, buttons and gesture
        // recognizers that mark pointer events handled on their way up - the drag still
        // has to see them, the same way Android's onInterceptTouchEvent sees every touch.
        AddHandler(PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
        AddHandler(PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
        AddHandler(PointerReleasedEvent, new PointerEventHandler(OnPointerReleased), true);
        AddHandler(PointerCanceledEvent, new PointerEventHandler(OnPointerCanceled), true);
        AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnPointerCaptureLost), true);
        AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;

        // Up/down scrolling stays with the page; a pointer drag already in progress wins.
        if (!properties.IsHorizontalMouseWheel || dragging)
            return;

        // A sideways list under the pointer scrolls itself instead (only when starting,
        // so a swipe already moving the pages isn't taken over by what it slides past).
        if (!wheelDragging && OwnsHorizontalDrag(e.OriginalSource as DependencyObject))
            return;

        // Still sliding from the last swipe (or a tab tap): starting now would only
        // pile up an offset CView ignores and then jump by once it settles.
        if (!wheelDragging && Owner?.Invoke() is { IsNavigating: true })
        {
            e.Handled = true;
            return;
        }

        // Positive is a swipe towards the right-hand content - the same as a finger
        // moving left, which is a negative dx for CView.
        wheelDx -= properties.MouseWheelDelta;
        wheelDragging = true;
        e.Handled = true;
        Owner?.Invoke()?.DragUpdate(wheelDx);

        if (wheelTimer == null)
        {
            wheelTimer = DispatcherQueue.CreateTimer();
            wheelTimer.IsRepeating = false;
            wheelTimer.Interval = TimeSpan.FromMilliseconds(WheelIdleMs);
            wheelTimer.Tick += (_, _) => EndWheelDrag();
        }

        // Restarted on every delta: the drag ends once they've stopped for a moment.
        wheelTimer.Stop();
        wheelTimer.Start();
    }

    private void EndWheelDrag()
    {
        if (!wheelDragging)
            return;

        var dx = wheelDx;
        wheelDragging = false;
        wheelDx = 0;
        Owner?.Invoke()?.DragEnd(dx);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (trackedPointer != null)
            return;

        var point = e.GetCurrentPoint(this);

        // Only the left mouse button - a right-click (context menu) or middle-click
        // isn't the start of a swipe.
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && !point.Properties.IsLeftButtonPressed)
            return;

        if (OwnsHorizontalDrag(e.OriginalSource as DependencyObject))
            return;

        trackedPointer = e.Pointer.PointerId;
        dragStart = point.Position;
        dragging = false;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (trackedPointer != e.Pointer.PointerId)
            return;

        var position = e.GetCurrentPoint(this).Position;
        double dx = position.X - dragStart.X;
        double dy = position.Y - dragStart.Y;

        if (!dragging)
        {
            if (Math.Abs(dx) < CView.TouchSlop && Math.Abs(dy) < CView.TouchSlop)
                return;

            // Mostly vertical (a scroll, or a drag meant for something else) - leave
            // this pointer alone until it goes down again.
            if (Math.Abs(dx) < Math.Abs(dy) * CView.DirectionLockRatio)
            {
                trackedPointer = null;
                return;
            }

            // Set before capturing: taking the capture makes whatever child had it
            // (a pressed button) lose it, and that PointerCaptureLost bubbles up here.
            dragging = true;
            CapturePointer(e.Pointer);
        }

        e.Handled = true;
        Owner?.Invoke()?.DragUpdate(dx);
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (trackedPointer != e.Pointer.PointerId)
            return;

        trackedPointer = null;

        if (!dragging)
            return;

        dragging = false;
        // A drag isn't a click: nothing under the pointer should act on the release.
        e.Handled = true;

        double dx = e.GetCurrentPoint(this).Position.X - dragStart.X;
        ReleasePointerCapture(e.Pointer);
        Owner?.Invoke()?.DragEnd(dx);
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (trackedPointer != e.Pointer.PointerId)
            return;

        CancelDrag();
    }

    // Before the drag starts, any lost capture (e.g. a ScrollViewer taking a touch pan
    // over natively) means the pointer is someone else's now. Once dragging, only this
    // container losing its own capture counts - children losing theirs to it is expected.
    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (trackedPointer != e.Pointer.PointerId)
            return;

        if (dragging && !ReferenceEquals(e.OriginalSource, this))
            return;

        CancelDrag();
    }

    private void CancelDrag()
    {
        trackedPointer = null;

        if (!dragging)
            return;

        dragging = false;
        Owner?.Invoke()?.DragCancel();
    }

    // Whether the pointer went down on something that needs a sideways drag itself:
    // text (selecting it), a slider or scrollbar, or a list that scrolls horizontally -
    // the Windows counterpart of the horizontal-scroll arbitration the other platforms'
    // handlers do.
    private bool OwnsHorizontalDrag(DependencyObject source)
    {
        for (var element = source; element != null && !ReferenceEquals(element, this); element = VisualTreeHelper.GetParent(element))
        {
            switch (element)
            {
                case TextBox or PasswordBox or RichEditBox or WinSlider or ScrollBar:
                    return true;
                case ScrollViewer scroll when scroll.HorizontalScrollMode != Microsoft.UI.Xaml.Controls.ScrollMode.Disabled && scroll.ScrollableWidth > 0:
                    return true;
            }
        }

        return false;
    }

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
