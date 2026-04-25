

namespace AppSkeleton;

public static class XeInitializer
{
    public static MauiAppBuilder UseAppSkeleton(this MauiAppBuilder builder)
    {
        return builder
            .UseAcrylicView();
    }
}