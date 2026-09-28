namespace AppSkeleton;

// A Label whose text the user can select and copy - for error messages, ids, addresses
// and anything else worth pasting elsewhere. A drop-in Label (every Label property and
// style still applies); selection is switched on per platform in CLabelInitializer:
// Android long-press selection with the system copy menu, Windows mouse/keyboard
// selection with Ctrl+C and the right-click Copy menu.
public class CLabel : Label
{
    public static readonly BindableProperty IsSelectableProperty =
        BindableProperty.Create(nameof(IsSelectable), typeof(bool), typeof(CLabel), true,
            propertyChanged: (bindable, _, _) => ((CLabel)bindable).Handler?.UpdateValue(nameof(IsSelectable)));

    public bool IsSelectable
    {
        get => (bool)GetValue(IsSelectableProperty);
        set => SetValue(IsSelectableProperty, value);
    }
}
