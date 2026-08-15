namespace AppSkeleton;

public static class CViewInitializer
{
    public static MauiAppBuilder UseCView(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID || WINDOWS || IOS || MACCATALYST
            handlers.AddHandler(typeof(CView), typeof(CViewHandler));
#endif
        });
        return builder;
    }
}
