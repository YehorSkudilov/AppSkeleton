using System;
using System.Collections.Generic;
using System.Text;

namespace AppSkeleton;

public abstract class PageBase : ContentView
{
    public virtual bool IsNavVisible=> true;
    protected PageBase()
    {
        Padding = new Thickness(10,0);
    }

    public Task InitialLoadTask { get; protected set; } = Task.CompletedTask;

    private DateTime lastRefreshUtc = DateTime.MinValue;

    protected bool ShouldRefresh(TimeSpan minInterval)
    {
        var now = DateTime.UtcNow;
        if (now - lastRefreshUtc < minInterval)
            return false;

        lastRefreshUtc = now;
        return true;
    }

    public virtual void Load()
    {

    }

    public virtual void Unload()
    {

    }

    public virtual Task RefreshAsync() => Task.CompletedTask;
}
