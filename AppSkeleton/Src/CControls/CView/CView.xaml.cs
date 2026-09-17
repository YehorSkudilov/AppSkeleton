using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AppSkeleton;

public partial class CView : ContentView, ICView
{
    private CNavItem currentCNavItem;
    public static readonly BindableProperty IsNavigatingProperty =
    BindableProperty.Create(
        nameof(IsNavigating),
        typeof(bool),
        typeof(CView),
        false);

    public bool IsNavigating
    {
        get => (bool)GetValue(IsNavigatingProperty);
        private set => SetValue(IsNavigatingProperty, value);
    }

    public static readonly BindableProperty CNavIconItemsProperty =
        BindableProperty.Create(
            nameof(CNavIconItems),
            typeof(ObservableCollection<CNavItem>),
            typeof(CView),
            default(ObservableCollection<CNavItem>));

    public ObservableCollection<CNavItem> CNavIconItems
    {
        get => (ObservableCollection<CNavItem>)GetValue(CNavIconItemsProperty);
        set => SetValue(CNavIconItemsProperty, value);
    }

    // Lets the swipe gesture below drive the same selection path a NavBar tap does
    // (highlighting included) instead of calling SetContent directly and leaving the
    // NavBar's selected icon out of sync with what's on screen.
    public static readonly BindableProperty SwipeNavigationCommandProperty =
        BindableProperty.Create(
            nameof(SwipeNavigationCommand),
            typeof(Command<CNavItem>),
            typeof(CView),
            default(Command<CNavItem>));

    public Command<CNavItem> SwipeNavigationCommand
    {
        get => (Command<CNavItem>)GetValue(SwipeNavigationCommandProperty);
        set => SetValue(SwipeNavigationCommandProperty, value);
    }

    const int animSpeed = 300;

    // A plain SwipeGestureRecognizer on MainGrid only ever fires over empty background,
    // since a MAUI gesture recognizer never gets a look-in once a foreground element -
    // and especially a scrollable native control (CollectionView/ScrollView) - claims the
    // touch stream first. CViewHandler instead does the gesture arbitration natively per
    // platform (Android onInterceptTouchEvent, iOS/MacCatalyst simultaneous-recognition
    // UIPanGestureRecognizer, Windows handledEventsToo pointer routing) and drives
    // DragUpdate/DragEnd/DragCancel below directly off the raw pointer stream once a
    // drag turns out to be horizontal-dominant past DirectionLockRatio/TouchSlop.
    internal const double DirectionLockRatio = 1.5;
    internal const double TouchSlop = 10;

    // A release faster than this (device-independent units/ms) commits or cancels the
    // swipe off its direction alone - a short, fast flick finishes the gesture even if
    // the finger barely travelled, the same way a native swipe-back does. Anything
    // slower falls back to RevertThreshold instead.
    internal const double FlingVelocityThreshold = 0.5;

    // Below a fling speed, only a release that lands back within this many units of
    // the start counts as the user having undone the gesture - everything past that
    // finishes the swipe instead of needing to cross the halfway point.
    internal const double RevertThreshold = 24;

    // The tab CarouselTo's already-running step loop is currently walking towards -
    // null when nothing is animating. A new SetContent() request while one step-loop
    // (or a drag - see DragUpdate/DragEnd) is already in flight just updates this
    // instead of being dropped or starting a second overlapping loop; the running loop
    // re-reads it every iteration (see CarouselTo), so it redirects toward the new
    // target as soon as its current single step finishes, rather than the caller
    // having to wait out the whole (possibly multi-tab) original animation first.
    private CNavItem desiredTarget;

    // ----- finger-driven drag state -----
    // Set once a horizontal drag actually starts (see DragUpdate) and cleared once it
    // settles (DragEnd/DragCancel) - lets DragEnd/DragCancel tell a genuine drag apart
    // from one that never got past the direction-lock decision (e.g. a tap, or a
    // vertical scroll), and guards DragUpdate against being reached twice for the
    // same drag.
    private bool isDragging;
    private int dragDirection;
    private double dragWidth;
    private View dragCurrentView;
    private View dragNextView;
    private CNavItem dragNextItem;

    // Instantaneous dx velocity (device-independent units/ms), refreshed on every
    // DragUpdate from the delta against the previous sample - used at release to tell
    // a short, fast flick apart from a slow drag (see FlingVelocityThreshold). Only
    // approximates true release velocity (there's usually no further move between the
    // last DragUpdate and the platform's own DragEnd), which is good enough for this -
    // a real fling's velocity carries over essentially unchanged in that tiny gap.
    private double dragVelocity;
    private double dragLastDx;
    private long dragLastUpdateTicks;

    // ----- finger-driven pull-to-refresh state -----
    // Currently only ever driven from Android (see SwipeInterceptingViewGroup/
    // CViewHandler there) - Android's SwipeRefreshLayout has its own native spinner
    // that can't be fully suppressed without a visible flash the moment it decides to
    // intercept, so pull-to-refresh is instead recognized here, the same way the
    // horizontal tab-swipe already is, with the native SwipeRefreshLayout disabled
    // entirely on that platform. Windows/iOS/MacCatalyst still drive pull-to-refresh
    // through RefreshView directly and never touch any of this.
    internal const double PullCommitDistance = 64;

    private bool isPulling;

    public event EventHandler<double> PullProgressChanged;
    public event EventHandler PullCommitted;
    public event EventHandler PullCancelled;

    public CView()
    {
        InitializeComponent();
    }

    // Called continuously while a vertical pull-to-refresh drag is in progress, with dy
    // the total vertical offset in device-independent units from wherever the finger
    // first went down - always >= 0 by construction (the native layer only ever starts
    // a pull for a downward drag while whatever's on screen is already scrolled to its
    // top). progress (dy / PullCommitDistance) is what callers drive their own visual
    // indicator off - 1.0 is "far enough that releasing now would commit".
    internal void PullUpdate(double dy)
    {
        if (IsNavigating && !isPulling)
            return;

        isPulling = true;

        double progress = Math.Max(0, dy) / PullCommitDistance;
        PullProgressChanged?.Invoke(this, progress);
    }

    // Called once the finger lifts - commits or cancels purely off release distance, no
    // fling shortcut like the horizontal drag has (a pull-to-refresh commit is a
    // deliberate "far enough", not a flick).
    internal void PullEnd(double dy)
    {
        if (!isPulling)
            return;

        isPulling = false;

        if (Math.Max(0, dy) >= PullCommitDistance)
            PullCommitted?.Invoke(this, EventArgs.Empty);
        else
            PullCancelled?.Invoke(this, EventArgs.Empty);
    }

    // Bounces back - used when the gesture is cancelled outright (see DragCancel's own
    // reasoning) rather than released normally.
    internal void PullCancel()
    {
        if (!isPulling)
            return;

        isPulling = false;
        PullCancelled?.Invoke(this, EventArgs.Empty);
    }

    // Called continuously while a horizontal drag is in progress (see the platform
    // CViewHandler/SwipeIntercepting* classes), with dx the total horizontal offset in
    // device-independent units from wherever the finger first went down - negative
    // means the finger moved left (advancing to the next tab), positive means it moved
    // right (back to the previous one), same ordering CarouselTo already animates
    // along. Moves the current page and whichever neighbor the drag is heading toward
    // 1:1 with the finger instead of only reacting once the drag finishes, so the
    // transition actually tracks the touch the whole way rather than only kicking off
    // a fixed-speed animation after the fact.
    internal void DragUpdate(double dx)
    {
        // A drag already in progress keeps updating even though it set IsNavigating
        // itself below - this only blocks *starting* a new drag while some other
        // navigation (a NavBar tap's CarouselTo) is already animating.
        if (IsNavigating && !isDragging)
            return;

        if (currentCNavItem == null || CNavIconItems == null)
            return;

        if (!isDragging)
        {
            dragWidth = MainGrid.Width > 0 ? MainGrid.Width : 300;
            dragCurrentView = currentCNavItem.Page;
            // 0 is a deliberate "no side chosen yet" sentinel (not a real direction) -
            // guarantees the very first nonzero dx below is treated as a change and
            // attaches a neighbor via SwitchDragNeighbor.
            dragDirection = 0;
            dragNextView = null;
            dragNextItem = null;

            isDragging = true;
            IsNavigating = true;

            dragVelocity = 0;
            dragLastDx = dx;
            dragLastUpdateTicks = Environment.TickCount64;
        }
        else
        {
            long now = Environment.TickCount64;
            long elapsedMs = now - dragLastUpdateTicks;

            if (elapsedMs > 0)
                dragVelocity = (dx - dragLastDx) / elapsedMs;

            dragLastDx = dx;
            dragLastUpdateTicks = now;
        }

        int desiredDirection = dx < 0 ? 1 : dx > 0 ? -1 : 0;

        // Lets the drag change its mind mid-gesture (drag toward the next tab, then
        // without lifting, back past the start and toward the previous one instead)
        // instead of being stuck with whichever neighbor was chosen first. Only
        // switches once the current page is actually back at its resting position
        // (TranslationX ~0) - if it switched while still partway toward the old
        // neighbor, the page would jump-cut instead of sliding smoothly.
        if (desiredDirection != 0 && desiredDirection != dragDirection &&
            (dragNextView == null || Math.Abs(dragCurrentView.TranslationX) < 0.5))
        {
            SwitchDragNeighbor(desiredDirection);
        }

        // At an edge with nothing further that way - leave the page fixed under the
        // finger instead of sliding it into blank space.
        if (dragNextView == null)
            return;

        // Clamped to one page width each way - past that there's nothing further to
        // reveal (only the one neighbor above is ever attached), so the drag just
        // stops moving the pages instead of over-shooting into empty space.
        double offset = dragDirection > 0
            ? Math.Clamp(dx, -dragWidth, 0)
            : Math.Clamp(dx, 0, dragWidth);

        dragCurrentView.TranslationX = offset;
        dragNextView.TranslationX = dragDirection * dragWidth + offset;
    }

    // Attaches (or swaps in) whichever neighbor lies in the given direction from
    // currentCNavItem, detaching whatever neighbor was previously attached for this
    // drag. Wraps around at either end - swiping backward past the first tab lands on
    // the last one and vice versa - the same way the tab strip itself has no real
    // "start"/"end", just wherever CNavIconItems happens to begin. Leaves dragNextView
    // null (with dragDirection still updated to the attempted direction) only when
    // there's nowhere at all to go (currentCNavItem missing, or a single-tab list).
    private void SwitchDragNeighbor(int direction)
    {
        if (dragNextView != null)
        {
            MainGrid.Children.Remove(dragNextView);
        }

        dragDirection = direction;

        int count = CNavIconItems.Count;
        int currentIndex = CNavIconItems.IndexOf(currentCNavItem);

        if (currentIndex < 0 || count < 2)
        {
            dragNextView = null;
            dragNextItem = null;
            return;
        }

        int nextIndex = ((currentIndex + direction) % count + count) % count;

        dragNextItem = CNavIconItems[nextIndex];
        dragNextView = dragNextItem.Page;

        EnsureDetached(dragNextView);
        dragNextView.TranslationX = dragDirection * dragWidth;
        MainGrid.Children.Add(dragNextView);
    }

    // Called once the finger lifts - animates the rest of the way to whichever side
    // (the page that was already on screen, or the incoming one) the drag was already
    // closer to when released, instead of snapping through a fixed animation unrelated
    // to where the release actually happened.
    internal async void DragEnd(double dx)
    {
        if (!isDragging)
            return;

        isDragging = false;

        var currentView = dragCurrentView;
        var nextView = dragNextView;
        var nextItem = dragNextItem;
        var direction = dragDirection;
        var width = dragWidth;
        double velocity = dragVelocity;
        dragCurrentView = null;
        dragNextView = null;
        dragNextItem = null;
        dragVelocity = 0;

        // Released while pinned at an edge with nothing attached to drag toward
        // (see SwitchDragNeighbor) - the page never actually moved, so there's
        // nothing to settle and no neighbor view to animate.
        if (nextView == null)
        {
            IsNavigating = false;

            if (desiredTarget != null && desiredTarget != currentCNavItem)
                await CarouselTo();

            return;
        }

        double offset = direction > 0
            ? Math.Clamp(dx, -width, 0)
            : Math.Clamp(dx, 0, width);

        // Both normalized so positive always means "further toward finishing the swipe"
        // regardless of which physical direction that is for this drag.
        double progress = direction > 0 ? -offset : offset;
        double normalizedVelocity = direction > 0 ? -velocity : velocity;

        bool commit;

        if (Math.Abs(normalizedVelocity) > FlingVelocityThreshold)
        {
            // A short, fast flick - finish (or cancel) the swipe off its direction
            // alone, the same way a native swipe-back gesture reacts to velocity
            // instead of final position.
            commit = normalizedVelocity > 0;
        }
        else
        {
            // A slow release - only bounce back to the page that was already on screen
            // once the finger has actually returned it almost to its starting spot;
            // anywhere past that finishes the swipe instead of needing to clear the
            // halfway point.
            commit = progress > RevertThreshold;
        }

        try
        {
            await SettleDragAsync(currentView, nextView, direction, width, offset, commit);
        }
        finally
        {
            IsNavigating = false;
        }

        if (commit)
        {
            MainGrid.Children.Remove(currentView);
            currentCNavItem = nextItem;
        }
        else
        {
            MainGrid.Children.Remove(nextView);
        }

        // A NavBar tap (or another SetContent call) that arrived mid-drag already ran
        // its own CAppShell bookkeeping synchronously at tap time and only got as far
        // as queuing itself in desiredTarget here (SetContent bails out early while
        // IsNavigating is true) - honor that redirect instead of reporting the drag's
        // own destination, the same way a redirect mid-CarouselTo takes priority over
        // whatever intermediate tab the carousel happened to be passing through.
        if (desiredTarget != null && desiredTarget != currentCNavItem)
        {
            await CarouselTo();
        }
        else if (commit)
        {
            // Fires after currentCNavItem/IsNavigating already reflect the finished
            // transition, so the CAppShell.SetContent this triggers just runs its
            // bookkeeping (nav history, Load(), PageName, TopBar refreshes) - its own
            // early-return sees currentCNavItem already equal to target and skips
            // starting a second, redundant carousel animation.
            SwipeNavigationCommand?.Execute(nextItem);
        }
    }

    // Bounces back to the page that was already on screen - used when the gesture is
    // cancelled outright (the OS reclaims the touch stream, the app is backgrounded
    // mid-drag, ...) rather than released normally, since there's no release position
    // to judge a "closest side" from at that point.
    internal async void DragCancel()
    {
        if (!isDragging)
            return;

        isDragging = false;

        var currentView = dragCurrentView;
        var nextView = dragNextView;
        var direction = dragDirection;
        var width = dragWidth;
        dragCurrentView = null;
        dragNextView = null;
        dragNextItem = null;

        // Pinned at an edge with nothing attached (see SwitchDragNeighbor) - the page
        // never moved, so there's nothing to bounce back from.
        if (nextView == null)
        {
            IsNavigating = false;

            if (desiredTarget != null && desiredTarget != currentCNavItem)
                await CarouselTo();

            return;
        }

        try
        {
            await SettleDragAsync(currentView, nextView, direction, width, currentView.TranslationX, commit: false);
        }
        finally
        {
            IsNavigating = false;
        }

        MainGrid.Children.Remove(nextView);

        if (desiredTarget != null && desiredTarget != currentCNavItem)
            await CarouselTo();
    }

    // Shared tail end of a finger-driven drag - animates from wherever it was released
    // to a fully-settled 0/±width offset, taking proportionally less time the closer
    // it already was to that side rather than always replaying the same fixed-speed
    // animation regardless of how far the finger had already carried it.
    private static Task SettleDragAsync(View currentView, View nextView, int direction, double width, double offset, bool commit)
    {
        double remaining = commit ? width - Math.Abs(offset) : Math.Abs(offset);
        uint duration = width > 0
            ? (uint)Math.Clamp(animSpeed * (remaining / width), 1, animSpeed)
            : (uint)animSpeed;

        double currentTarget = commit ? -direction * width : 0;
        double nextTarget = commit ? 0 : direction * width;

        return Task.WhenAll(
            currentView.TranslateToAsync(currentTarget, 0, duration, Easing.CubicOut),
            nextView.TranslateToAsync(nextTarget, 0, duration, Easing.CubicOut));
    }

    public async void SetContent(CNavItem target)
    {
        if (currentCNavItem == null || !CNavIconItems.Contains(currentCNavItem) || !CNavIconItems.Contains(target))
        {
            // Instant swap (no carousel step to redirect) - only valid with nothing
            // already animating, since it bypasses the step loop entirely.
            if (IsNavigating)
                return;

            currentCNavItem = target;

            EnsureDetached(currentCNavItem.Page);

            // CarouselTo leaves the outgoing view translated off-screen when it removes
            // it (see the TranslateTo calls below) - if that same view later comes back
            // through this instant-swap path instead of another carousel, it'd still
            // carry that stale offset and render invisible. Reset it explicitly.
            currentCNavItem.Page.TranslationX = 0;
            currentCNavItem.Page.TranslationY = 0;

            MainGrid.Children.Clear();
            MainGrid.Add(currentCNavItem.Page);

            return;
        }

        if (currentCNavItem == target && desiredTarget == null)
            return;

        desiredTarget = target;

        // A loop is already running toward (what is now) the old target - it re-reads
        // desiredTarget every iteration, so updating it above is enough; don't start a
        // second overlapping CarouselTo.
        if (IsNavigating)
            return;

        await CarouselTo();
    }

    private async Task CarouselTo()
    {
        if (IsNavigating)
            return;

        IsNavigating = true;

        try
        {
            bool isFirstStep = true;

            while (currentCNavItem != desiredTarget)
            {
                int currentIndex = CNavIconItems.IndexOf(currentCNavItem);
                int targetIndex = CNavIconItems.IndexOf(desiredTarget);

                if (currentIndex < 0 || targetIndex < 0)
                    return;

                // Recomputed live off the current step, not captured once up front -
                // a redirect mid-flight changes how many steps actually remain, so the
                // "total time stays equal to animSpeed" split has to follow whatever
                // the remaining distance to desiredTarget is right now.
                int remainingSteps = Math.Abs(targetIndex - currentIndex);
                uint stepDuration = (uint)Math.Max(1, animSpeed / remainingSteps);

                int direction = Math.Sign(targetIndex - currentIndex);
                int nextIndex = currentIndex + direction;

                if (nextIndex < 0 || nextIndex >= CNavIconItems.Count)
                    return;

                bool isLastStep = nextIndex == targetIndex;

                Easing easing;

                if (isFirstStep && isLastStep)
                {
                    easing = Easing.CubicInOut;
                }
                else if (isFirstStep)
                {
                    easing = Easing.CubicIn;
                }
                else if (isLastStep)
                {
                    easing = Easing.CubicOut;
                }
                else
                {
                    easing = Easing.Linear;
                }

                var nextItem = CNavIconItems[nextIndex];

                var currentView = currentCNavItem.Page;
                var nextView = nextItem.Page;

                EnsureDetached(nextView);

                double width = MainGrid.Width;
                if (width <= 0)
                    width = 300;

                nextView.TranslationX = direction * width;

                MainGrid.Children.Add(nextView);

                Debug.WriteLine(
                    $"[CView] CarouselTo step {currentIndex}->{nextIndex}: width={width}, " +
                    $"currentView.TranslationX(start)={currentView.TranslationX}, " +
                    $"nextView.TranslationX(start)={nextView.TranslationX}, stepDuration={stepDuration}");

                var currentAnim = currentView.TranslateToAsync(
                    -direction * width,
                    0,
                    stepDuration,
                    easing);

                var nextAnim = nextView.TranslateToAsync(
                    0,
                    0,
                    stepDuration,
                    easing);

                await Task.WhenAll(currentAnim, nextAnim);

                Debug.WriteLine(
                    $"[CView] CarouselTo step {currentIndex}->{nextIndex} settled: " +
                    $"currentView.TranslationX(end)={currentView.TranslationX}, " +
                    $"nextView.TranslationX(end)={nextView.TranslationX}");

                MainGrid.Children.Remove(currentView);

                currentCNavItem = nextItem;
                isFirstStep = false;
            }
        }
        catch (Exception ex)
        {
            // CarouselTo runs off SetContent, which is async void - an exception here
            // would otherwise vanish silently as an unobserved exception (crashing the
            // app or just getting swallowed depending on platform), leaving whatever
            // page was mid-transition stuck exactly where it was, with the old page
            // never removed from MainGrid.Children - logged here so that failure mode
            // is at least visible instead of a silent stuck/overlapping carousel.
            Debug.WriteLine($"[CView] CarouselTo failed: {ex}");
            throw;
        }
        finally
        {
            IsNavigating = false;
            desiredTarget = null;
        }
    }

    private void EnsureDetached(View view)
    {
        if (view?.Parent is Layout parent)
        {
            parent.Children.Remove(view);
        }
    }
}