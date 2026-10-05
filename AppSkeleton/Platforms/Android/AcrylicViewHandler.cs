using Android.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Color = Microsoft.Maui.Graphics.Color;
using View = Android.Views.View;

namespace AppSkeleton;

public partial class AcrylicViewHandler : ViewHandler<IAcrylicView, FrameLayout>
{
    private RealtimeBlurView realtimeBlurView;
    private BorderDrawable colorGradientDrawable;
    private View colorBlendLayer;
    private float colorBlendLayerAlpha = 0f;
    private BorderViewGroup borderViewGroup;

    protected override FrameLayout CreatePlatformView()
    {
        colorBlendLayer = new View(Context);

        realtimeBlurView = new RealtimeBlurView(Context, SetContentVisible);
        realtimeBlurView.SetBlurRadius(120);
        realtimeBlurView.SetOverlayColor(Colors.Transparent.ToPlatform());
        realtimeBlurView.SetDownsampleFactor(4);

        borderViewGroup = new BorderViewGroup(Context)
        {
            CrossPlatformMeasure = (w, h) =>
            {
                if (VirtualView == null) return Size.Zero;
                try { return VirtualView.CrossPlatformMeasure(w, h); }
                catch { return Size.Zero; }
            },
            CrossPlatformArrange = (rect) =>
            {
                if (VirtualView == null) return Size.Zero;
                try { return VirtualView.CrossPlatformArrange(rect); }
                catch { return Size.Zero; }
            }
        };

        var frame = new ContentSizedFrame(Context) { Content = borderViewGroup };
        frame.AddView(realtimeBlurView);
        frame.AddView(colorBlendLayer);
        frame.AddView(borderViewGroup);

        return frame;
    }

    // A plain FrameLayout sizes itself to its largest child, and the blur and tint layers are plain views that take
    // all the room they're offered - so an AcrylicView that should hug its content (a pill, a centred card) grew to
    // fill its whole parent. This measures the content alone, then fits the other layers to it; an exact size from
    // the parent (e.g. a full-width bar) is still used as given.
    private sealed class ContentSizedFrame(Android.Content.Context context) : FrameLayout(context)
    {
        public View Content { get; set; }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            if (Content == null)
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
                return;
            }

            Content.Measure(widthMeasureSpec, heightMeasureSpec);
            var width = MeasureSpec.GetMode(widthMeasureSpec) == Android.Views.MeasureSpecMode.Exactly
                ? MeasureSpec.GetSize(widthMeasureSpec)
                : Content.MeasuredWidth;
            var height = MeasureSpec.GetMode(heightMeasureSpec) == Android.Views.MeasureSpecMode.Exactly
                ? MeasureSpec.GetSize(heightMeasureSpec)
                : Content.MeasuredHeight;

            var exactWidth = MeasureSpec.MakeMeasureSpec(width, Android.Views.MeasureSpecMode.Exactly);
            var exactHeight = MeasureSpec.MakeMeasureSpec(height, Android.Views.MeasureSpecMode.Exactly);
            for (var i = 0; i < ChildCount; i++)
                GetChildAt(i)?.Measure(exactWidth, exactHeight);

            SetMeasuredDimension(width, height);
        }
    }

    // Called around each blur capture, so the content isn't blurred into its own background.
    private void SetContentVisible(bool isVisible)
    {
        if (borderViewGroup == null) return;
        borderViewGroup.SkipDraw = !isVisible;
    }

    private static void MapTintColor(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;
        if (view.EffectStyle != EffectStyle.Custom) return;
        handler.UpdateColorblendLayer(view);
    }

    private static void MapTintOpacity(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;
        if (handler.colorBlendLayer == null) return;
        if (view.EffectStyle != EffectStyle.Custom) return;
        if (view.TintColor == null || view.TintColor == Colors.Transparent) return;

        handler.colorBlendLayerAlpha = (float)view.TintOpacity;
        handler.colorBlendLayer.Alpha = handler.colorBlendLayerAlpha;
    }

    private static void MapEffectStyle(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;

        switch (view.EffectStyle)
        {
            case EffectStyle.Dark:
                handler.UpdateEffectStyle(view, Colors.Black, 0.15f);
                break;
            case EffectStyle.ExtraDark:
                handler.UpdateEffectStyle(view, Colors.Black, 0.3f);
                break;
            case EffectStyle.Light:
                handler.UpdateEffectStyle(view, Colors.White, 0.05f);
                break;
            case EffectStyle.ExtraLight:
                handler.UpdateEffectStyle(view, Colors.White, 0.3f);
                break;
            case EffectStyle.Custom:
                handler.UpdateColorblendLayer(view);
                break;
        }
    }

    private void UpdateEffectStyle(IAcrylicView view, Color color, float tintOpacity)
    {
        if (colorBlendLayer == null) return;

        colorGradientDrawable = new BorderDrawable(Context, view.CornerRadius, color.ToPlatform());
        colorBlendLayer.SetBackgroundDrawable(colorGradientDrawable);

        colorBlendLayerAlpha = tintOpacity;
        colorBlendLayer.Alpha = colorBlendLayerAlpha;
    }

    private static void MapContent(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;
        if (handler.borderViewGroup == null) return;

        handler.borderViewGroup.RemoveAllViews();

        if (view.Content is IView content && view.Handler?.MauiContext != null)
        {
            try
            {
                var platformView = ElementExtensions.ToPlatform(content, view.Handler.MauiContext);
                handler.borderViewGroup.AddView(platformView);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AcrylicViewHandler] MapContent failed: {ex.Message}");
            }
        }
    }

    private static void MapBorderThickness(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;
        if (handler.realtimeBlurView == null) return;

        handler.realtimeBlurView.SetBorderThickness(view.BorderThickness);
        PropertyChanged(handler, view);
    }

    private static void MapCornerRadius(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;
        if (handler.realtimeBlurView == null) return;

        handler.UpdateColorblendLayer(view);

        var thickness = handler.PlatformView.Context.ToPixels(view.CornerRadius);
        handler.realtimeBlurView.SetCornerRadius(
            (float)thickness.Left,
            (float)thickness.Top,
            (float)thickness.Right,
            (float)thickness.Bottom);

        PropertyChanged(handler, view);
    }

    private static void MapBorderColor(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;
        PropertyChanged(handler, view);
    }

    private static void PropertyChanged(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler?.PlatformView == null) return;
        if (handler.borderViewGroup == null) return;

        handler.borderViewGroup.BorderDrawable = new BorderDrawable(handler.PlatformView.Context, view);
    }

    private void UpdateColorblendLayer(IAcrylicView view)
    {
        if (colorBlendLayer == null) return;

        if ((view.TintColor == null || view.TintColor == Colors.Transparent)
            && view.EffectStyle == EffectStyle.Custom)
        {
            colorBlendLayer.SetBackgroundDrawable(null);
        }
        else
        {
            if (view.TintColor == null) return;

            colorGradientDrawable = new BorderDrawable(Context, view.CornerRadius, view.TintColor.ToPlatform());
            colorBlendLayer.SetBackgroundDrawable(colorGradientDrawable);

            colorBlendLayerAlpha = (float)view.TintOpacity;
            colorBlendLayer.Alpha = colorBlendLayerAlpha;
        }
    }

    protected override void DisconnectHandler(FrameLayout platformView)
    {
        realtimeBlurView?.Destroy();
        realtimeBlurView = null;
        colorBlendLayer = null;
        borderViewGroup = null;
        colorGradientDrawable = null;

        base.DisconnectHandler(platformView);
    }
}