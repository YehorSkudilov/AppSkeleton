using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace AppSkeleton;

public partial class AcrylicViewHandler : ViewHandler<IAcrylicView, BorderView>
{
    //颜色层
    private UIView colorBlendUIView;

    private UIVisualEffectView acrylicEffectView;

    protected override BorderView CreatePlatformView()
    {
        var borderView = new BorderView
        {
            CrossPlatformMeasure = new Func<double, double, Size>(VirtualView.CrossPlatformMeasure),
            CrossPlatformArrange = new Func<Rect, Size>(VirtualView.CrossPlatformArrange)
        };

        colorBlendUIView = [];

        acrylicEffectView = new UIVisualEffectView()
        {
            Frame = VirtualView.Frame,
            ClipsToBounds = true,
            // Custom (the default): no material at all, just TintColor - as on Android, where
            // Custom is a plain blur plus the tint. Every iOS blur material lays its own grey
            // or white over the view, which washed the apps' green backgrounds out.
            Effect = null
        };

        return borderView;
    }

    static void MapTintColor(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler != null)
            handler.colorBlendUIView.BackgroundColor = view.TintColor.ToPlatform();
    }

    static void MapTintOpacity(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler != null)
            handler.colorBlendUIView.Alpha = (float)view.TintOpacity;
    }
    static void MapBorderColor(AcrylicViewHandler handler, IAcrylicView view)
    {
        var nativView = handler?.PlatformView;
        if (nativView == null) return;
        nativView.BorderColor = view.BorderColor.ToCGColor();
    }
    static void MapBorderThickness(AcrylicViewHandler handler, IAcrylicView view)
    {
        var nativView = handler?.PlatformView;
        if (nativView == null) return;

        nativView.BorderThickness = view.BorderThickness;
    }

    static void MapCornerRadius(AcrylicViewHandler handler, IAcrylicView view)
    {
        var nativView = handler?.PlatformView;
        if (nativView == null) return;
        nativView.CornerRadius = view.CornerRadius;
    }

    static void MapContent(AcrylicViewHandler handler, IAcrylicView view)
    {
        var nativView = handler?.PlatformView;
        if (nativView == null) return;

        Microsoft.Maui.Platform.ViewExtensions.ClearSubviews(nativView);

        //加入亚克力层
        handler.acrylicEffectView.Frame = UIScreen.MainScreen.Bounds;
        nativView.AddSubview(handler.acrylicEffectView);

        ////加入颜色层
        handler.colorBlendUIView.Frame = UIScreen.MainScreen.Bounds;
        nativView.AddSubview(handler.colorBlendUIView);

        //加入Maui视图
        if (view.PresentedContent is IView content && view.Handler != null)
        {
            var frameworkElement = content.ToPlatform(view.Handler.MauiContext);
            nativView.AddSubview(frameworkElement);
        }
    }

    private static void MapEffectStyle(AcrylicViewHandler handler, IAcrylicView view)
    {
        if (handler == null)
            return;
        if (view.EffectStyle == EffectStyle.Custom)
        {
            handler.acrylicEffectView.Effect = null;
            return;
        }

        // var osVersion = UIDevice.CurrentDevice.SystemVersion;

        //if (!float.TryParse(ver, out float version))
        //    return;

        var style = view.EffectStyle switch
        {
            EffectStyle.Light => UIBlurEffectStyle.Light,
            EffectStyle.Dark => UIBlurEffectStyle.Dark,
            EffectStyle.ExtraLight => UIBlurEffectStyle.ExtraLight,
            EffectStyle.ExtraDark => UIBlurEffectStyle.Dark,  //ExtraDark仅支持tvos
            _ => UIBlurEffectStyle.Light
        };
        handler.acrylicEffectView.Effect = UIBlurEffect.FromStyle(style);
    }
}