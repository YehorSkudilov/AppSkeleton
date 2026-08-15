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
    // UIPanGestureRecognizer, Windows handledEventsToo pointer routing) and calls Swipe()
    // directly once a horizontal drag past DirectionLockRatio/SwipeThreshold completes.
    internal const double SwipeThreshold = 60;
    internal const double DirectionLockRatio = 1.5;
    internal const double TouchSlop = 10;

    public CView()
    {
        InitializeComponent();
    }

    // Swiping left moves forward to the next CNavIconItems entry, swiping right moves
    // back to the previous one - same ordering CarouselTo already animates along.
    internal void Swipe(int direction)
    {
        if (IsNavigating || currentCNavItem == null || CNavIconItems == null)
            return;

        int currentIndex = CNavIconItems.IndexOf(currentCNavItem);

        // Not one of the tab pages (e.g. a Settings sub-page) - nothing to carousel to.
        if (currentIndex < 0)
            return;

        int targetIndex = currentIndex + direction;

        if (targetIndex < 0 || targetIndex >= CNavIconItems.Count)
            return;

        SwipeNavigationCommand?.Execute(CNavIconItems[targetIndex]);
    }

    public async void SetContent(CNavItem target)
    {
        if (IsNavigating)
            return;

        if (currentCNavItem == null || !CNavIconItems.Contains(currentCNavItem) || !CNavIconItems.Contains(target))
        {
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
        else
        {
            if (currentCNavItem == target)
                return;

            await CarouselTo(target);
        }

    }

    private async Task CarouselTo(CNavItem target)
    {
        if (IsNavigating)
            return;

        IsNavigating = true;

        try
        {
            int startIndex = CNavIconItems.IndexOf(currentCNavItem);
            int targetIndex = CNavIconItems.IndexOf(target);

            if (startIndex < 0 || targetIndex < 0)
                return;

            int totalSteps = Math.Abs(targetIndex - startIndex);

            if (totalSteps == 0)
                return;

            bool isFirstStep = true;

            // Total animation time stays equal to animSpeed
            uint stepDuration = (uint)Math.Max(1, animSpeed / totalSteps);

            while (currentCNavItem != target)
            {
                int currentIndex = CNavIconItems.IndexOf(currentCNavItem);

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