using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Color = Android.Graphics.Color;
using Math = System.Math;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using RectF = Android.Graphics.RectF;
using View = Android.Views.View;

namespace AppSkeleton;

public class RealtimeBlurView : View
{
    private readonly float[] mRadii = new float[8];
    private float mDownsampleFactor;
    private int mOverlayColor;
    private float mBlurRadius;
    private readonly IBlurImpl mBlurImpl;
    private bool mDirty;
    private Bitmap mBitmapToBlur, mBlurredBitmap;
    private Canvas mBlurringCanvas;
    private bool mIsRendering;
    private readonly Paint mPaint = new();
    private JniWeakReference<View> _weakDecorView;
    private bool mDifferentRoot;
    private bool _isContainerShown = true;
    private bool _autoUpdate = true;
    private static int RENDERING_COUNT;
    private static int BLUR_IMPL;
    private Thickness borderThickness = new();

    public Action<bool> SetContentVisible;

    [Obsolete("此类库 在>=Android12 已经不再使用，谷歌已经更新了一套新的模糊操作类库")]
    public RealtimeBlurView(Context context, Action<bool> visibleAction, string formsId = null) : base(context)
    {
        mBlurImpl = GetBlurImpl();
        preDrawListener = new PreDrawListener(this);
        SetContentVisible = visibleAction;
    }

    public RealtimeBlurView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public void SetBorderThickness(Thickness borderThickness)
    {
        this.borderThickness = borderThickness;
        // Guard: only trigger pre-draw if listener and action are ready
        if (preDrawListener != null && SetContentVisible != null)
            preDrawListener.OnPreDraw(borderThickness, SetContentVisible);
    }

    protected IBlurImpl GetBlurImpl()
    {
        try
        {
            AndroidStockBlurImpl impl = new();
            Bitmap bmp = Bitmap.CreateBitmap(4, 4, Bitmap.Config.Argb8888);
            impl.Prepare(Context, bmp, 4);
            impl.Release();
            bmp.Recycle();
            BLUR_IMPL = 3;
        }
        catch { }

        if (BLUR_IMPL == 0)
            BLUR_IMPL = -1;

        return BLUR_IMPL switch
        {
            3 => new AndroidStockBlurImpl(),
            _ => new EmptyBlurImpl(),
        };
    }

    public void SetDownsampleFactor(float factor)
    {
        if (factor <= 0)
            throw new ArgumentException("Downsample factor must be greater than 0.");

        if (mDownsampleFactor != factor)
        {
            mDownsampleFactor = factor;
            mDirty = true;
            ReleaseBitmap();
            Invalidate();
        }
    }

    private void SubscribeToPreDraw(View decorView)
    {
        if (decorView.IsNullOrDisposed() || decorView.ViewTreeObserver.IsNullOrDisposed()) return;
        decorView.ViewTreeObserver.AddOnPreDrawListener(preDrawListener);
    }

    private void UnsubscribeToPreDraw(View decorView)
    {
        if (decorView.IsNullOrDisposed() || decorView.ViewTreeObserver.IsNullOrDisposed()) return;
        decorView.ViewTreeObserver.RemoveOnPreDrawListener(preDrawListener);
    }

    public void Destroy()
    {
        if (_weakDecorView != null && _weakDecorView.TryGetTarget(out var mDecorView))
            UnsubscribeToPreDraw(mDecorView);

        Release();
        _weakDecorView = null;
    }

    public void Release()
    {
        SetRootView(null);
        ReleaseBitmap();
        mBlurImpl?.Release();
    }

    public void SetBlurRadius(float radius, bool invalidate = true)
    {
        if (mBlurRadius == radius) return;
        mBlurRadius = radius;
        mDirty = true;
        if (invalidate)
            Invalidate();
    }

    public void SetOverlayColor(int color, bool invalidate = true)
    {
        if (mOverlayColor == color) return;
        mOverlayColor = color;
        if (invalidate)
            Invalidate();
    }

    public void SetRootView(View rootView)
    {
        var mDecorView = GetRootView();
        if (mDecorView != rootView)
        {
            UnsubscribeToPreDraw(mDecorView);
            _weakDecorView = new JniWeakReference<View>(rootView);
            if (IsAttachedToWindow)
                OnAttached(rootView);
        }
    }

    private View GetRootView()
    {
        View mDecorView = null;
        _weakDecorView?.TryGetTarget(out mDecorView);
        return mDecorView;
    }

    private void OnAttached(View mDecorView)
    {
        if (mDecorView != null)
        {
            using var handler = new Handler(Looper.MainLooper);
            handler.PostDelayed(() =>
            {
                SubscribeToPreDraw(mDecorView);
                mDifferentRoot = mDecorView.RootView != RootView;
                if (mDifferentRoot)
                    mDecorView.PostInvalidate();
            }, 8);
        }
        else
        {
            mDifferentRoot = false;
        }
    }

    protected override void OnVisibilityChanged(View changedView, [GeneratedEnum] ViewStates visibility)
    {
        base.OnVisibilityChanged(changedView, visibility);

        if (changedView.GetType().Name == "PageContainer")
        {
            _isContainerShown = visibility == ViewStates.Visible;
            SetAutoUpdate(_isContainerShown);
        }
    }

    private void SetAutoUpdate(bool autoUpdate)
    {
        if (autoUpdate) EnableAutoUpdate();
        else DisableAutoUpdate();
    }

    private void EnableAutoUpdate()
    {
        if (_autoUpdate) return;
        _autoUpdate = true;
        using var handler = new Handler(Looper.MainLooper);
        handler.PostDelayed(() =>
        {
            var mDecorView = GetRootView();
            if (mDecorView == null || !_autoUpdate) return;
            SubscribeToPreDraw(mDecorView);
        }, 50);
    }

    private void DisableAutoUpdate()
    {
        if (!_autoUpdate) return;
        _autoUpdate = false;
        var mDecorView = GetRootView();
        if (mDecorView == null) return;
        UnsubscribeToPreDraw(mDecorView);
    }

    private void ReleaseBitmap()
    {
        if (!mBitmapToBlur.IsNullOrDisposed())
        {
            mBitmapToBlur.Recycle();
            mBitmapToBlur = null;
        }
        if (!mBlurredBitmap.IsNullOrDisposed())
        {
            mBlurredBitmap.Recycle();
            mBlurredBitmap = null;
        }
    }

    protected bool Prepare()
    {
        if (mBlurRadius == 0)
        {
            Release();
            return false;
        }

        float downsampleFactor = mDownsampleFactor;
        float radius = mBlurRadius / downsampleFactor;
        if (radius > 25)
        {
            downsampleFactor = downsampleFactor * radius / 25;
            radius = 25;
        }

        int width = Width;
        int height = Height;

        // Guard: skip if dimensions are not valid yet
        if (width <= 0 || height <= 0) return false;

        int scaledWidth = Math.Max(1, (int)(width / downsampleFactor));
        int scaledHeight = Math.Max(1, (int)(height / downsampleFactor));
        bool dirty = mDirty;

        if (mBlurringCanvas == null || mBlurredBitmap == null
            || mBlurredBitmap.Width != scaledWidth || mBlurredBitmap.Height != scaledHeight)
        {
            dirty = true;
            ReleaseBitmap();
            bool r = false;
            try
            {
                mBitmapToBlur = Bitmap.CreateBitmap(scaledWidth, scaledHeight, Bitmap.Config.Argb8888);
                if (mBitmapToBlur == null) return false;

                mBlurringCanvas = new Canvas(mBitmapToBlur);
                mBlurredBitmap = Bitmap.CreateBitmap(scaledWidth, scaledHeight, Bitmap.Config.Argb8888);
                if (mBlurredBitmap == null) return false;

                r = true;
            }
            catch { }
            finally
            {
                if (!r) Release();
            }

            if (!r) return false;
        }

        if (dirty)
        {
            if (mBlurImpl.Prepare(Context, mBitmapToBlur, radius))
                mDirty = false;
            else
                return false;
        }

        return true;
    }

    protected void Blur(Bitmap bitmapToBlur, Bitmap blurredBitmap)
    {
        mBlurImpl.Blur(bitmapToBlur, blurredBitmap);
    }

    private readonly PreDrawListener preDrawListener;

    private class PreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        private readonly JniWeakReference<RealtimeBlurView> _weakBlurView;
        private readonly double _density;
        private Thickness _borderThickness = new();
        private Action<bool> SetContentVisible;

        public PreDrawListener(RealtimeBlurView blurView)
        {
            _weakBlurView = new JniWeakReference<RealtimeBlurView>(blurView);
            _density = DeviceDisplay.Current.MainDisplayInfo.Density;
        }

        public PreDrawListener(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        public void OnPreDraw(Thickness thickness, Action<bool> contentVisibleAction)
        {
            _borderThickness = thickness;
            SetContentVisible = contentVisibleAction;
            OnPreDraw();
        }

        // Capturing means drawing everything behind the view in software, every frame something on screen changes - the
        // costliest thing the app does while scrolling. The blur is wide and tinted, so a capture at most this often
        // looks the same; a frame skipped for it schedules one later capture, so it still ends on what's behind it now.
        private const long MinCaptureIntervalMs = 33;
        private long _lastCaptureTicks;
        private bool _catchUpPosted;
        private readonly Android.Graphics.Rect _visibleRect = new();

        public bool OnPreDraw()
        {
            // Guard: action not yet wired up
            if (SetContentVisible == null) return true;

            if (!_weakBlurView.TryGetTarget(out var blurView)) return false;

            // Guard: view dimensions not valid yet — skip this draw pass entirely
            if (blurView.Width <= 0 || blurView.Height <= 0) return true;

            if (!blurView._isContainerShown)
                return false;

            // Not on screen (a tab parked off to the side, or scrolled away): nothing to show, so nothing to capture.
            if (!blurView.IsShown || !blurView.GetGlobalVisibleRect(_visibleRect))
                return true;

            long now = System.Environment.TickCount64;
            long since = now - _lastCaptureTicks;
            if (since < MinCaptureIntervalMs)
            {
                if (!_catchUpPosted)
                {
                    _catchUpPosted = true;
                    blurView.PostInvalidateDelayed(MinCaptureIntervalMs - since);
                }
                return true;
            }
            _lastCaptureTicks = now;
            _catchUpPosted = false;

            // Hides the view's own content from the capture. A flag its content checks while drawing, not a change to
            // its alpha, which would invalidate it and so start another frame (and another capture) every time.
            SetContentVisible(false);
            try
            {
                Capture(blurView);
            }
            finally
            {
                SetContentVisible(true);
            }
            return true;
        }

        private static void Capture(RealtimeBlurView blurView)
        {
            var mDecorView = blurView.GetRootView();
            int[] locations = new int[2];
            Bitmap oldBmp = blurView.mBlurredBitmap;
            View decor = mDecorView;

            if (!decor.IsNullOrDisposed() && blurView.IsShown && blurView.Prepare())
            {
                bool redrawBitmap = blurView.mBlurredBitmap != oldBmp;

                decor.GetLocationOnScreen(locations);
                blurView.GetLocationOnScreen(locations);

                blurView.mBitmapToBlur.EraseColor(Color.Transparent);
                int rc = blurView.mBlurringCanvas.Save();
                blurView.mIsRendering = true;
                RENDERING_COUNT++;
                try
                {
                    float sx = (float)blurView.mBitmapToBlur.Width / blurView.Width;
                    float sy = (float)blurView.mBitmapToBlur.Height / blurView.Height;

                    blurView.mBlurringCanvas.Scale(sx, sy);
                    blurView.mBlurringCanvas.Translate(-locations[0], -locations[1]);
                    decor.Background?.Draw(blurView.mBlurringCanvas);
                    decor.Draw(blurView.mBlurringCanvas);
                }
                finally
                {
                    blurView.mIsRendering = false;
                    RENDERING_COUNT--;
                    blurView.mBlurringCanvas.RestoreToCount(rc);
                }

                blurView.Blur(blurView.mBitmapToBlur, blurView.mBlurredBitmap);

                if (redrawBitmap || blurView.mDifferentRoot)
                    blurView.Invalidate();
            }
        }
    }

    protected View GetActivityDecorView()
    {
        Context ctx = Context;
        for (int i = 0; i < 4 && ctx != null && ctx is not Activity && ctx is ContextWrapper wrapper; i++)
            ctx = wrapper.BaseContext;

        return (ctx is Activity activity) ? activity.Window.DecorView : null;
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        var mDecorView = GetRootView();
        if (mDecorView == null)
            SetRootView(GetActivityDecorView());
        else
            OnAttached(mDecorView);
    }

    protected override void OnDetachedFromWindow()
    {
        var mDecorView = GetRootView();
        if (mDecorView != null)
            UnsubscribeToPreDraw(mDecorView);

        Release();
        base.OnDetachedFromWindow();
    }

    public override void Draw(Canvas canvas)
    {
        if (mIsRendering) return;
        if (RENDERING_COUNT <= 0)
            base.Draw(canvas);
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        DrawRoundedBlurredBitmap(canvas, mBlurredBitmap);
    }

    private void DrawRoundedBlurredBitmap(Canvas canvas, Bitmap blurredBitmap)
    {
        if (blurredBitmap != null)
        {
            var mRectF = new RectF { Right = Width, Bottom = Height };
            mPaint.Reset();
            mPaint.AntiAlias = true;
            var shader = new BitmapShader(blurredBitmap, Shader.TileMode.Clamp, Shader.TileMode.Clamp);
            var matrix = new Matrix();
            matrix.PostScale(mRectF.Width() / blurredBitmap.Width, mRectF.Height() / blurredBitmap.Height);
            shader.SetLocalMatrix(matrix);
            mPaint.SetShader(shader);
            var path2 = new Path();
            path2.AddRoundRect(mRectF, mRadii, Path.Direction.Cw);
            canvas.DrawPath(path2, mPaint);
        }
    }

    public void SetCornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        mDirty = true;
        mRadii[0] = topLeft; mRadii[1] = topLeft;
        mRadii[2] = topRight; mRadii[3] = topRight;
        mRadii[4] = bottomRight; mRadii[5] = bottomRight;
        mRadii[6] = bottomLeft; mRadii[7] = bottomLeft;
        Invalidate();
    }

    protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
    {
        base.OnSizeChanged(w, h, oldw, oldh);

        // Guard: only proceed when both dimensions are valid AND action is ready
        if (w > 0 && h > 0 && preDrawListener != null && SetContentVisible != null)
            preDrawListener.OnPreDraw(borderThickness, SetContentVisible);
    }
}