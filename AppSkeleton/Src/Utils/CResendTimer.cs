using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppSkeleton;

/// <summary>
/// A "Resend code" button's countdown: a server that sends one emailed code a minute per address means that after a
/// code goes out the button counts down, "Resend code (0:42)", and only works again at zero. When the server refuses
/// one anyway and says how long to wait (Retry-After), the count takes that instead (<see cref="Refused"/>).
/// Bind a button's Text to <see cref="Text"/> and IsEnabled to <see cref="CanResend"/>.
/// </summary>
public class CResendTimer : INotifyPropertyChanged
{
    /// <summary>The wait between two codes to one address (match the server's).</summary>
    public static TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The button's words for the seconds left (0: it can be tapped). Set once at start-up by an app that translates
    /// its text; the default is English.
    /// </summary>
    public static Func<int, string> Format { get; set; } = seconds => seconds == 0
        ? "Resend code"
        : $"Resend code ({seconds / 60}:{seconds % 60:00})";

    DateTime _until;
    CancellationTokenSource? _ticking;
    int _secondsLeft;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int SecondsLeft
    {
        get => _secondsLeft;
        private set
        {
            if (_secondsLeft == value)
                return;
            _secondsLeft = value;
            Changed();
            Changed(nameof(Text));
            Changed(nameof(CanResend));
        }
    }

    public bool CanResend => SecondsLeft == 0;

    public string Text => Format(SecondsLeft);

    /// <summary>A code just went out: the next one in <see cref="Cooldown"/>.</summary>
    public void Sent() => Start(Cooldown);

    /// <summary>
    /// After a refused send: the server's wait when it said one (Retry-After), else a full cooldown when it said
    /// "too many requests" (429). Other failures leave the button as it is.
    /// </summary>
    public void Refused(TimeSpan? retryAfter, bool tooManyRequests)
    {
        if (retryAfter is { } wait)
            Start(wait);
        else if (tooManyRequests)
            Start(Cooldown);
    }

    /// <summary>Counts down from <paramref name="wait"/> (zero or less stops it). Called on the main thread.</summary>
    public void Start(TimeSpan wait)
    {
        _ticking?.Cancel();
        _until = DateTime.UtcNow + wait;
        SecondsLeft = Remaining();
        if (SecondsLeft == 0)
            return;
        _ticking = new CancellationTokenSource();
        _ = TickAsync(_ticking.Token);
    }

    /// <summary>Stops the count: the button works straight away.</summary>
    public void Reset() => Start(TimeSpan.Zero);

    int Remaining() => Math.Max(0, (int)Math.Ceiling((_until - DateTime.UtcNow).TotalSeconds));

    async Task TickAsync(CancellationToken ct)
    {
        try
        {
            while (SecondsLeft > 0)
            {
                // To the next whole second, so the count doesn't drift.
                var toNext = (_until - DateTime.UtcNow).TotalMilliseconds % 1000;
                await Task.Delay(TimeSpan.FromMilliseconds(toNext <= 0 ? 1000 : toNext), ct);
                SecondsLeft = Remaining();
            }
        }
        catch (OperationCanceledException)
        {
            // Restarted with a new wait.
        }
    }

    void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
