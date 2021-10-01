using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seq.App.Slack.Api;
using Seq.App.Slack.Formatting;
using Seq.Apps;
using Seq.Apps.LogEvents;
// ReSharper disable PossibleMultipleEnumeration

namespace Seq.App.Slack.Messages
{
    class AlertV2MessageBuilder : SlackMessageBuilder
    {
        private readonly Host _host;
        private readonly PropertyValueFormatter _propertyValueFormatter;
        private readonly string _messageTemplate;
        private static readonly HashSet<string> SpecialProperties = new HashSet<string>(new[] { "NamespacedAlertTitle", "Alert", "Source", "SuppressedUntil", "Failures" });

        public AlertV2MessageBuilder(Host host, Apps.App app, PropertyValueFormatter propertyValueFormatter, string channel, string username, string messageTemplate, string iconUrl, bool excludeOptionalAttachments) 
            : base(app, channel, username, iconUrl, excludeOptionalAttachments)
        {
            _host = host;
            _propertyValueFormatter = propertyValueFormatter ?? throw new ArgumentNullException(nameof(propertyValueFormatter));
            _messageTemplate = messageTemplate;
        }

        protected override string GenerateMessageText(Event<LogEventData> evt)
        {
            var namespacedAlertTitle = EventFormatting.SafeGetProperty(evt, "NamespacedAlertTitle");
            var alertUrl = EventFormatting.SafeGetProperty(evt, "Alert.Url");
            return $"Alert condition triggered by {SlackSyntax.Hyperlink(alertUrl, namespacedAlertTitle)}";
        }

        protected override void AddNecessaryAttachments(SlackMessage message, Event<LogEventData> evt, string color)
        {
            var resultsUrl = EventFormatting.SafeGetProperty(evt, "Source.ResultsUrl");
            var resultsText = SlackSyntax.Hyperlink(resultsUrl, "Explore detected results in Seq");
            var results = new SlackMessageAttachment(color, resultsText);
            message.Attachments.Add(results);

            if (_messageTemplate != null)
            {
                message.Attachments.Add(new SlackMessageAttachment(color, _messageTemplate, null, true));
            }

            if (evt.Data.Properties.TryGetValue("Failures", out var f) &&
                f is IEnumerable<object> failures)
            {
                foreach (var failure in failures)
                {
                    var failed = new SlackMessageAttachment(color, SlackSyntax.Escape(failure?.ToString() ?? ""), "Alert Processing Failed");
                    message.Attachments.Add(failed);
                }
            }
            
            var notificationProperties = new SlackMessageAttachment(color);
            foreach (var property in evt.Data.Properties)
            {
                if (SpecialProperties.Contains(property.Key)) continue;
                var value = _propertyValueFormatter.ConvertPropertyValueToString(property.Value);
                notificationProperties.Fields.Add(new SlackMessageAttachmentField(property.Key, value, @short: false));
            }
                
            if (notificationProperties.Fields.Count != 0)
                message.Attachments.Add(notificationProperties);

            // Contributing events are opted-in per notification, so they're considered minimal (the user can configure
            // the alert to exclude them if desired).
            if (evt.Data.Properties.TryGetValue("Source", out var r) &&
                r is IReadOnlyDictionary<string, object> rd &&
                rd.TryGetValue("ContributingEvents", out var ce) &&
                ce is IEnumerable<object> contributingEvents &&
                contributingEvents.Count() > 1)
            {
                var text = new StringBuilder();
                foreach (var contributing in contributingEvents.Skip(1).Cast<IEnumerable<object>>())
                {
                    var columns = contributing.Cast<string>().ToArray();
                    
                    // Timestamp as ISO-8601 string
                    text.Append(SlackSyntax.Code(columns[1]));
                    text.Append(" ");
                    
                    // Message, linking to event
                    text.Append(SlackSyntax.Hyperlink(EventFormatting.LinkToId(_host, columns[0]), SlackSyntax.Escape(columns[2])));
                    text.Append("\n");
                }
                
                var events = new SlackMessageAttachment(color, text.ToString(), "Contributing Events");
                message.Attachments.Add(events);
            }
        }
    }
}
