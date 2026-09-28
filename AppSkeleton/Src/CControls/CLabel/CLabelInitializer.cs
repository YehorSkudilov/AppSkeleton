using Microsoft.Maui.Handlers;

namespace AppSkeleton;

public static class CLabelInitializer
{
    // Hooks into the stock LabelHandler rather than a handler of its own, so CLabel keeps
    // every bit of Label's native rendering; only CLabel instances are touched.
    public static MauiAppBuilder UseCLabel(this MauiAppBuilder builder)
    {
        LabelHandler.Mapper.AppendToMapping(nameof(CLabel.IsSelectable), Apply);
        // Setting text or text type can reset the native view's selection behaviour, so re-apply after them.
        LabelHandler.Mapper.AppendToMapping(nameof(Label.Text), Apply);
        LabelHandler.Mapper.AppendToMapping(nameof(Label.TextType), Apply);
        LabelHandler.Mapper.AppendToMapping(nameof(Label.FormattedText), Apply);
        return builder;
    }

    static void Apply(ILabelHandler handler, ILabel view)
    {
        if (view is not CLabel label)
            return;
#if ANDROID
        // Long press selects text and shows the system Copy / Select all menu.
        handler.PlatformView.SetTextIsSelectable(label.IsSelectable);
#elif WINDOWS
        // Drag or double-click to select; Ctrl+C and the right-click menu copy.
        handler.PlatformView.IsTextSelectionEnabled = label.IsSelectable;
#endif
    }
}
