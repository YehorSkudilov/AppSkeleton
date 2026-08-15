namespace AppSkeleton;

// Typing the platform handlers against this instead of the concrete CView class
// avoids CS0618: Layout.CrossPlatformMeasure/CrossPlatformArrange are obsolete on
// the concrete class (superseded by MeasureOverride/ArrangeOverride) but the
// IContentView members of the same name aren't - same reason AcrylicViewHandler
// is typed against IAcrylicView rather than AcrylicView.
public interface ICView : IView, IContentView
{
}
