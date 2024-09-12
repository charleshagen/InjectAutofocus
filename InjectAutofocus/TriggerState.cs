using NINA.Core.Utility;
using System;
using System.Threading;

namespace CharlesHagen.NINA.InjectAutofocus;

internal static class TriggerState {
    private static int _triggerState;
    private static DateTime _lastRequested;
    private static object _lock = new();

    public static int GetTriggerState() {
        lock (_lock) { 
            return _triggerState; 
        }
    }

    public static DateTime GetLastRequested() { 
        lock (_lock) {
            return _lastRequested;
        }
    }

    public static void RequestAutofocus() {
        lock (_lock) {
            _lastRequested = DateTime.Now;
            Logger.Info($"Inject Autofocus Requested at {_lastRequested.ToString("yyyy-MM-dd HH:mm:ss")}");
            Interlocked.Increment(ref _triggerState);
        }
    }

    public static void Reset() {
        Interlocked.Exchange(ref _triggerState, 0);
    }
}
