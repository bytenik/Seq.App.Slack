using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Slack
{
    class AlertV2MessageBuilder : SlackMessageBuilder
    {
        private readonly string _messageTemplate;

        public AlertV2MessageBuilder(Apps.App app, string channel, string username, string messageTemplate, string iconUrl, bool excludeAttachments) 
            : base(app, channel, username, iconUrl, excludeAttachments)
        {
            _messageTemplate = messageTemplate;
        }

        protected override string GenerateMessageText(Event<LogEventData> evt)
        {
            var namespacedAlertTitle = EventFormatting.SafeGetProperty(evt, "NamespacedAlertTitle");
            var alertUrl = EventFormatting.SafeGetProperty(evt, "Alert.Url");
            return $"Alert condition triggered by {SlackSyntax.Hyperlink(alertUrl, namespacedAlertTitle)}";
        }

        protected override void AddAttachments(SlackMessage message, Event<LogEventData> evt, string color)
        {
            var resultsUrl = EventFormatting.SafeGetProperty(evt, "Source.ResultsUrl");
            var resultsText = SlackSyntax.Hyperlink(resultsUrl, "Explore detected results in Seq");
            var results = new SlackMessageAttachment(color, resultsText);
            message.Attachments.Add(results);
            
            // Contributing events should be included here.

            if (_messageTemplate != null)
            {
                message.Attachments.Add(new SlackMessageAttachment(color, _messageTemplate, null, true));
            }
        }
    }
}
