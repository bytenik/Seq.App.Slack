using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Serilog;

namespace Seq.App.Slack
{
    static class EventFormatting
    {
        private static readonly Regex PlaceholdersRegex = new Regex(@"(\[(?<key>[^\[\]]+?)(\:(?<format>[^\[\]]+?))?\])", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly IImmutableDictionary<LogEventLevel, string> LevelColorMap = new Dictionary<LogEventLevel, string>
        {
            [LogEventLevel.Verbose] = "#D3D3D3",
            [LogEventLevel.Debug] = "#D3D3D3",
            [LogEventLevel.Information] = "#00A000",
            [LogEventLevel.Warning] = "#f9c019",
            [LogEventLevel.Error] = "#e03836",
            [LogEventLevel.Fatal] = "#e03836",
        }.ToImmutableDictionary();

        public static string LevelToColor(LogEventLevel level)
        {
            return LevelColorMap[level];
        }

        public static string SafeGetProperty(Event<LogEventData> evt, string propertyName, bool raw = false)
        {
            if (evt.Data.Properties.TryGetValue(propertyName, out var value))
            {
                if (value == null) return "`null`";
                return raw ? value.ToString() : SlackSyntax.Escape(value.ToString());
            }
            return "";
        }

        public static string SubstitutePlaceholders(string messageTemplateToUse, Event<LogEventData> evt, bool addLogData = true)
        {
            var data = evt.Data;
            var eventType = evt.EventType;
            var level = data.Level;
            
            var placeholders = new Dictionary<string, object>();
            if(data.Properties != null)
                foreach (var kvp in data.Properties)
                    placeholders[kvp.Key.ToLower()] = kvp.Value;

            if (addLogData)
            {
                AddValueIfKeyDoesNotExist(placeholders, "Level", level);
                AddValueIfKeyDoesNotExist(placeholders, "EventType", eventType);
                AddValueIfKeyDoesNotExist(placeholders, "RenderedMessage", data.RenderedMessage);
                AddValueIfKeyDoesNotExist(placeholders, "Exception", data.Exception);
            }
            return PlaceholdersRegex.Replace(messageTemplateToUse, m =>
            {
                var key = m.Groups["key"].Value.ToLower();
                var format = m.Groups["format"].Value;
                return placeholders.ContainsKey(key) ? FormatValue(placeholders[key], format) : m.Value;
            });
        }

        private static string FormatValue(object value, string format)
        {
            var rawValue = value?.ToString() ?? SlackSyntax.Code("null");

            if (String.IsNullOrWhiteSpace(format))
                return rawValue;

            try
            {
                return String.Format(format, rawValue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not format Slack message: {Value} {Format}", value, format);
            }

            return rawValue;
        }

        private static void AddValueIfKeyDoesNotExist(IDictionary<string, object> placeholders, string key, object value)
        {
            var loweredKey = key.ToLower();
            if (!placeholders.ContainsKey(loweredKey))
                placeholders.Add(loweredKey, value);
        }
    }
}
