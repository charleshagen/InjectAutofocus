using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CharlesHagen.NINA.InjectAutofocus;

internal static class TriggerState {

    private static int _triggerState;
    private static object _lock = new();

    public static int GetTriggerState() {
        lock (_lock) { 
            return _triggerState; 
        }
    }

    public static void RequestAutofocus() {
        Interlocked.Increment(ref _triggerState);
    }

    public static void Reset() {
        Interlocked.Exchange(ref _triggerState, 0);
    }
}
