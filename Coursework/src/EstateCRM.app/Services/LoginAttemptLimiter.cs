using System;

namespace EstateCRM.App.Services;

// Учебное ограничение действует только в запущенном экземпляре приложения.
public sealed class LoginBlockedException : Exception
{
    public LoginBlockedException(string message) : base(message) { }
}

public sealed class LoginAttemptLimiter
{
    private readonly TimeProvider _clock;
    private int _failures;
    private bool _blocked;
    private long _blockedAt;
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(30);

    public LoginAttemptLimiter(TimeProvider? clock = null) => _clock = clock ?? TimeProvider.System;

    public void EnsureAllowed()
    {
        if (!_blocked) return;
        var remaining = Duration - _clock.GetElapsedTime(_blockedAt);
        if (remaining > TimeSpan.Zero)
            throw new LoginBlockedException($"Вход временно заблокирован после трёх неудачных попыток. Повторите через {Math.Ceiling(remaining.TotalSeconds)} сек.");
        _blocked = false;
        _failures = 0;
    }

    public void RecordFailure()
    {
        _failures++;
        if (_failures < 3) return;
        _blocked = true;
        _blockedAt = _clock.GetTimestamp();
        throw new LoginBlockedException("Вход заблокирован на 30 секунд после трёх неудачных попыток.");
    }

    public void RecordSuccess()
    {
        _failures = 0;
        _blocked = false;
    }
}
