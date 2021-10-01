using System;
using System.Collections.Generic;
using System.Linq;

namespace Seq.App.Slack.Suppression
{
    class EventTypeSuppressions
    {
        private readonly Dictionary<uint, DateTime> _suppressions = new Dictionary<uint, DateTime>();
        private readonly int _suppressionMinutes;

        public EventTypeSuppressions(int suppressionMinutes)
        {
            _suppressionMinutes = suppressionMinutes;
        }

        public bool ShouldSuppressAt(uint eventType, DateTime utcNow)
        {
            if (_suppressionMinutes == 0)
                return false;

            if (!_suppressions.TryGetValue(eventType, out var suppressedSince) ||
                suppressedSince.AddMinutes(_suppressionMinutes) < utcNow)
            {
                // Not suppressed, or suppression expired

                // Clean up old entries
                var expired = _suppressions.FirstOrDefault(kvp => kvp.Value.AddMinutes(_suppressionMinutes) < utcNow);
                while (expired.Value != default)
                {
                    _suppressions.Remove(expired.Key);
                    expired = _suppressions.FirstOrDefault(kvp => kvp.Value.AddMinutes(_suppressionMinutes) < utcNow);
                }

                // Start suppression again
                suppressedSince = utcNow;
                _suppressions[eventType] = suppressedSince;
                return false;
            }

            // Suppressed
            return true;
        }
    }
}
