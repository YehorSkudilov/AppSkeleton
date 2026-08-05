using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AppSkeleton;

public partial class CView : ContentView
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

    const int animSpeed = 300;

    public CView()
    {
        InitializeComponent();
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