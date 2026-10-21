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
        // Android 12+ blurs with a RenderEffect instead (UseRenderNode), so RenderScript isn't started there.
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
            return new EmptyBlurImpl();

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
        ReleaseBlurNode();
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

    /// <summary>
    /// The least time between two captures of what's behind (before Android 12, where each is a software redraw of
    /// the window). 33ms, about 30 a second, by default; an app on slow hardware (a TV box) can capture less often.
    /// </summary>
    public static long MinCaptureIntervalMs { get; set; } = 33;

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
        // looks the same (see MinCaptureIntervalMs); a frame skipped for it schedules one later capture, so it still
        // ends on what's behind it now.
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

            // Android 12+: recording only references what's behind (no pixels are drawn here), so it's cheap enough for
            // every frame and needs neither the throttle nor hiding the content.
            if (blurView.UseRenderNode)
            {
                blurView.RecordBehind();

                // Drawn before it had anything recorded (see _blurNodeMissed): drawn once more, now that it has.
                if (blurView._blurNodeMissed)
                {
                    blurView._blurNodeMissed = false;
                    blurView.Invalidate();
                }
                return true;
            }

            long now = System.Environment.TickCount64;
            long since = now - _lastCaptureTicks;
            if (since < RealtimeBlurView.MinCaptureIntervalMs)
            {
                if (!_catchUpPosted)
                {
                    _catchUpPosted = true;
                    blurView.PostInvalidateDelayed(RealtimeBlurView.MinCaptureIntervalMs - since);
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
        _ancestorsKnown = false;
        var mDecorView = GetRootView();
        if (mDecorView == null)
            SetRootView(GetActivityDecorView());
        else
            OnAttached(mDecorView);
    }

    protected override void OnDetachedFromWindow()
    {
        _ancestorsKnown = false;
        _ancestors.Clear();
        _siblingsBefore.Clear();
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
        if (UseRenderNode)
            DrawRoundedBlurNode(canvas);
        else
            DrawRoundedBlurredBitmap(canvas, mBlurredBitmap);
    }

    // Android 12+ blurs on the GPU: what's behind the view is recorded into a RenderNode and drawn through a blur
    // RenderEffect. The software capture below it drew the whole window on the main thread for every capture, and its
    // bitmaps made the render thread upload every image on screen again each frame - most of the app's dropped frames.
    private bool UseRenderNode => OperatingSystem.IsAndroidVersionAtLeast(31) && IsHardwareAccelerated;

    private RenderNode _blurNode;
    private float _blurNodeRadius = -1;
    private Path _clipPath;
    private readonly List<View> _ancestors = [];
    private readonly int[] _location = new int[2];
    private readonly int[] _rootLocation = new int[2];

    // Records only what's drawn beneath this view: going down from the window's root, each ancestor's background and
    // the siblings before the next one on the way that reach under it (the blur is clipped to the view, so nothing
    // else shows). Not the whole window, which would hold this view and so draw itself. Each sibling's own drawing is
    // recorded again every frame, but what's inside it is drawn by reference to its own render nodes.
    private void RecordBehind()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return;
        int width = Width, height = Height;
        if (width <= 0 || height <= 0) return;

        _blurNode ??= new RenderNode("AcrylicBlur");
        _blurNode.SetPosition(0, 0, width, height);
        if (_blurNodeRadius != mBlurRadius)
        {
            _blurNodeRadius = mBlurRadius;
            // About the softness of the software blur at the same setting (it blurred a downsampled copy).
            float radius = mBlurRadius * 0.72f;
            _blurNode.SetRenderEffect(radius > 0 ? RenderEffect.CreateBlurEffect(radius, radius, Shader.TileMode.Clamp) : null);
        }

        UpdateAncestors();
        _ancestors[0].GetLocationInWindow(_rootLocation);
        GetLocationInWindow(_location);

        _siblingNodesUsed = 0;
        var canvas = _blurNode.BeginRecording(width, height);
        try
        {
            canvas.Translate(_rootLocation[0] - _location[0], _rootLocation[1] - _location[1]);
            for (int i = 0; i < _ancestors.Count - 1; i++)
            {
                var parent = (ViewGroup)_ancestors[i];
                var next = _ancestors[i + 1];
                parent.Background?.Draw(canvas);
                canvas.Translate(-parent.ScrollX, -parent.ScrollY);
                foreach (var child in _siblingsBefore[i])
                    if (Overlaps(child))
                        DrawBeneath(canvas, child);

                canvas.Translate(next.Left, next.Top);
                ApplyTransform(canvas, next);
            }
        }
        finally
        {
            _blurNode.EndRecording();
        }

        // Updating the node changes what an already-drawn view shows without a new frame of its own, but a view whose
        // only draw came before the first recording drew nothing (no display list yet) and was never asked to draw
        // again - a button that otherwise never changes (a floating +) stayed see-through for good, while ones that
        // redraw anyway (the nav bar, on every selection) looked fine. So draw again until the node has been drawn once.
        if (!_blurNodeDrawn)
            Invalidate();
    }

    private bool _blurNodeDrawn;

    // The way from the root down to this view, and at each step the children drawn before the next one: kept between
    // recordings, as finding them again went through Java for every view on the way each frame. Made again when this
    // view is attached or detached (anything on the way moving takes it out of the window) or a step gains or loses
    // children.
    private readonly List<List<View>> _siblingsBefore = [];
    private readonly List<int> _childCounts = [];
    private bool _ancestorsKnown;

    private void UpdateAncestors()
    {
        if (_ancestorsKnown)
        {
            int i = 0;
            for (; i < _childCounts.Count; i++)
                if (((ViewGroup)_ancestors[i]).ChildCount != _childCounts[i])
                    break;
            if (i == _childCounts.Count)
                return;
        }

        _ancestors.Clear();
        for (View v = this; v != null; v = v.Parent as View)
            _ancestors.Add(v);
        _ancestors.Reverse();

        _siblingsBefore.Clear();
        _childCounts.Clear();
        for (int i = 0; i < _ancestors.Count - 1; i++)
        {
            // A view's parent view is always a ViewGroup.
            var parent = (ViewGroup)_ancestors[i];
            var next = _ancestors[i + 1];
            var before = new List<View>();
            int count = parent.ChildCount;
            for (int j = 0; j < count; j++)
            {
                var child = parent.GetChildAt(j);
                if (child == next) break;
                if (child != null) before.Add(child);
            }
            _siblingsBefore.Add(before);
            _childCounts.Add(count);
        }
        _ancestorsKnown = true;
    }

    // A view's own translation, scale and rotation, as its parent applies them. Read as numbers rather than through
    // its Matrix, which makes a new Java object to hand over every time, unless it's actually scaled or rotated.
    private static void ApplyTransform(Canvas canvas, View view)
    {
        if (view.ScaleX == 1 && view.ScaleY == 1 && view.Rotation == 0 && view.RotationX == 0 && view.RotationY == 0)
        {
            float tx = view.TranslationX, ty = view.TranslationY;
            if (tx != 0 || ty != 0) canvas.Translate(tx, ty);
            return;
        }
        var matrix = view.Matrix;
        if (!matrix.IsIdentity) canvas.Concat(matrix);
    }

    private readonly int[] _siblingLocation = new int[2];

    // Whether the sibling reaches under this view, in window coordinates (_location is this view's, set by RecordBehind).
    private bool Overlaps(View child)
    {
        if (child == null || child.Width <= 0 || child.Height <= 0) return false;
        // Swapped for another since the way down was found (same number of children): find it again next time.
        if (!child.IsAttachedToWindow)
        {
            _ancestorsKnown = false;
            return false;
        }
        child.GetLocationInWindow(_siblingLocation);
        return _siblingLocation[0] < _location[0] + Width && _location[0] < _siblingLocation[0] + child.Width
            && _siblingLocation[1] < _location[1] + Height && _location[1] < _siblingLocation[1] + child.Height;
    }

    // One node per sibling drawn, reused from recording to recording.
    private readonly List<RenderNode> _siblingNodes = [];
    private int _siblingNodesUsed;

    // Each sibling gets a canvas of its own size, as its parent would give it: some views size what they draw by the
    // canvas (a MAUI Border's clip does).
    private void DrawBeneath(Canvas canvas, View child)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return;
        if (child == null || child.Visibility != ViewStates.Visible || child.Alpha <= 0f || child.Width <= 0 || child.Height <= 0)
            return;

        if (_siblingNodesUsed == _siblingNodes.Count)
            _siblingNodes.Add(new RenderNode("AcrylicBlurSibling"));
        var node = _siblingNodes[_siblingNodesUsed++];
        node.SetPosition(0, 0, child.Width, child.Height);
        node.SetAlpha(child.Alpha);

        var childCanvas = node.BeginRecording(child.Width, child.Height);
        try
        {
            // The content scrolled, the background (which View.Draw offsets back) not.
            childCanvas.Translate(-child.ScrollX, -child.ScrollY);
            child.Draw(childCanvas);
        }
        finally
        {
            node.EndRecording();
        }

        int save = canvas.Save();
        canvas.Translate(child.Left, child.Top);
        ApplyTransform(canvas, child);
        canvas.DrawRenderNode(node);
        canvas.RestoreToCount(save);
    }

    // Set when the view was drawn while nothing was recorded yet: just attached (the recording starts a moment
    // later), or brought in from off-screen (nothing is recorded there). Recording later doesn't draw the view
    // again - only a node it already drew shows the new recording - so without drawing it once more, its blur
    // stayed blank until something else happened to redraw it (the floating acrylic buttons on the pages never did).
    private bool _blurNodeMissed;

    private void DrawRoundedBlurNode(Canvas canvas)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return;
        if (!canvas.IsHardwareAccelerated) return;
        if (_blurNode == null || !_blurNode.HasDisplayList)
        {
            _blurNodeMissed = true;
            return;
        }
        _blurNodeMissed = false;

        _blurNodeDrawn = true;
        _clipPath ??= new Path();
        _clipPath.Reset();
        _clipPath.AddRoundRect(0, 0, Width, Height, mRadii, Path.Direction.Cw);
        int save = canvas.Save();
        canvas.ClipPath(_clipPath);
        canvas.DrawRenderNode(_blurNode);
        canvas.RestoreToCount(save);
    }

    private void ReleaseBlurNode()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return;
        _blurNode?.DiscardDisplayList();
        _blurNodeDrawn = false;
        foreach (var node in _siblingNodes)
            node.DiscardDisplayList();
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