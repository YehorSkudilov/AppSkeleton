using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AppSkeleton;

public partial class CView : ContentView
{
    private CNavItem currentCNavItem;
    private bool _isAnimating;

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
        if (_isAnimating)
            return;

        if (currentCNavItem == null)
        {
            currentCNavItem = target;

            EnsureDetached(currentCNavItem.Content);
            MainGrid.Children.Clear();
            MainGrid.Add(currentCNavItem.Content);

            return;
        }

        if (currentCNavItem == target)
            return;

        await CarouselTo(target);
    }

    private async Task CarouselTo(CNavItem target)
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        try
        {
            while (currentCNavItem != target)
            {
                int currentIndex = CNavIconItems.IndexOf(currentCNavItem);
                int targetIndex = CNavIconItems.IndexOf(target);

                if (currentIndex < 0 || targetIndex < 0)
                    return;

                int direction = Math.Sign(targetIndex - currentIndex);

                int nextIndex = currentIndex + direction;

                if (nextIndex < 0 || nextIndex >= CNavIconItems.Count)
                    return;

                var nextItem = CNavIconItems[nextIndex];

                var currentView = currentCNavItem.Content;
                var nextView = nextItem.Content;

                // IMPORTANT: avoid WinUI parent conflict
                EnsureDetached(nextView);

                double width = MainGrid.Width;
                if (width <= 0)
                    width = 300; // fallback safe value

                nextView.TranslationX = direction * width;

                MainGrid.Children.Add(nextView);

                uint duration = (uint)(animSpeed);

                var currentAnim = currentView.TranslateTo(-direction * width, 0, duration);
                var nextAnim = nextView.TranslateTo(0, 0, duration);

                await Task.WhenAll(currentAnim, nextAnim);

                MainGrid.Children.Remove(currentView);

                currentCNavItem = nextItem;
            }
        }
        finally
        {
            _isAnimating = false;
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