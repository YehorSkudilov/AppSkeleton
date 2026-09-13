namespace AppSkeleton;

// Gives CView's native root the ability to arbitrate a horizontal swipe against a
// descendant scrollable control (CollectionView/ScrollView) instead of losing the
// touch stream to it outright - see CView.xaml.cs's DragUpdate/DragEnd/DragCancel and
// the platform-specific CreatePlatformView() overrides for the actual gesture logic.
public partial class CViewHandler
{
    private static readonly PropertyMapper<ICView, CViewHandler> propertyMapper = new()
    {
        [nameof(ICView.Content)] = MapContent,
    };

    public CViewHandler() : base(propertyMapper)
    {
    }

    public CViewHandler(PropertyMapper mapper) : base(mapper)
    {
    }
}
