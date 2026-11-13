using System;
namespace FluentPrayerTimes;

internal sealed class ReloadPolicy
{
    private readonly TimeProvider _clock;
    private long? _last;
    private int _failures;
    internal bool Running { get; private set; }
    internal ReloadPolicy(TimeProvider? clock = null) => _clock = clock ?? TimeProvider.System;
    internal bool Due => !Running && (!_last.HasValue || _clock.GetElapsedTime(_last.Value) >= TimeSpan.FromSeconds(Math.Min(300, 5 * Math.Pow(2, Math.Min(_failures, 6)))));
    internal bool Begin() { if (Running) return false; Running = true; _last = _clock.GetTimestamp(); return true; }
    internal void Finish(bool success) { Running = false; _failures = success ? 0 : Math.Min(_failures + 1, 6); }
}
