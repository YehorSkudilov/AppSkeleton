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

    public CView()
    {
        InitializeComponent();
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
            int direction = dx < 0 ? 1 : -1;
            int currentIndex = CNavIconItems.IndexOf(currentCNavItem);
            if (currentIndex < 0)
                return;

            int nextIndex = currentIndex + direction;

            // At the first/last tab with nothing further that way - nothing to drag
            // toward, so leave the page fixed under the finger instead of sliding it
            // into blank space.
            if (nextIndex < 0 || nextIndex >= CNavIconItems.Count)
                return;

            dragWidth = MainGrid.Width > 0 ? MainGrid.Width : 300;
            dragDirection = direction;
            dragNextItem = CNavIconItems[nextIndex];
            dragCurrentView = currentCNavItem.Content;
            dragNextView = dragNextItem.Content;

            EnsureDetached(dragNextView);
            dragNextView.TranslationX = dragDirection * dragWidth;
            MainGrid.Children.Add(dragNextView);

            isDragging = true;
            IsNavigating = true;
        }

        // Clamped to one page width each way - past that there's nothing further to
        // reveal (only the one neighbor above is ever attached), so the drag just
        // stops moving the pages instead of over-shooting into empty space.
        double offset = dragDirection > 0
            ? Math.Clamp(dx, -dragWidth, 0)
            : Math.Clamp(dx, 0, dragWidth);

        dragCurrentView.TranslationX = offset;
        dragNextView.TranslationX = dragDirection * dragWidth + offset;
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
        dragCurrentView = null;
        dragNextView = null;
        dragNextItem = null;

        double offset = direction > 0
            ? Math.Clamp(dx, -width, 0)
            : Math.Clamp(dx, 0, width);

        bool commit = Math.Abs(offset) > width / 2;

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
            currentView.TranslateTo(currentTarget, 0, duration, Easing.CubicOut),
            nextView.TranslateTo(nextTarget, 0, duration, Easing.CubicOut));
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

            EnsureDetached(currentCNavItem.Content);

            // CarouselTo leaves the outgoing view translated off-screen when it removes
            // it (see the TranslateTo calls below) - if that same view later comes back
            // through this instant-swap path instead of another carousel, it'd still
            // carry that stale offset and render invisible. Reset it explicitly.
            currentCNavItem.Content.TranslationX = 0;
            currentCNavItem.Content.TranslationY = 0;

            MainGrid.Children.Clear();
            MainGrid.Add(currentCNavItem.Content);

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

                var currentView = currentCNavItem.Content;
                var nextView = nextItem.Content;

                EnsureDetached(nextView);

                double width = MainGrid.Width;
                if (width <= 0)
                    width = 300;

                nextView.TranslationX = direction * width;

                MainGrid.Children.Add(nextView);

                var currentAnim = currentView.TranslateTo(
                    -direction * width,
                    0,
                    stepDuration,
                    easing);

                var nextAnim = nextView.TranslateTo(
                    0,
                    0,
                    stepDuration,
                    easing);

                await Task.WhenAll(currentAnim, nextAnim);

                MainGrid.Children.Remove(currentView);

                currentCNavItem = nextItem;
                isFirstStep = false;
            }
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