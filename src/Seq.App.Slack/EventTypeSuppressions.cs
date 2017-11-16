using System;
using System.Collections.Concurrent;

namespace Seq.App.Slack
{
    class EventTypeSuppressions
    {
        private readonly ConcurrentDictionary<uint, DateTime> _lastSeen = new ConcurrentDictionary<uint, DateTime>();
        private readonly int _suppressionMinutes;

        public EventTypeSuppressions(int suppressionMinutes)
        {
            this._suppressionMinutes = suppressionMinutes;
        }

        public bool ShouldSuppressAt(uint eventType, DateTime utcNow)
        {
            var added = false;
            var lastSeen = _lastSeen.GetOrAdd(eventType, k => { added = true; return DateTime.UtcNow; });
            if (!added)
            {
                if (lastSeen > utcNow.AddMinutes(-_suppressionMinutes))
                    return true;

                _lastSeen[eventType] = utcNow;
            }

            return false;
        }
    }
}
