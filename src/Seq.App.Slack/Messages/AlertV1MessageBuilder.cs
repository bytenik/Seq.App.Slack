using Seq.App.Slack.Api;
using Seq.App.Slack.Formatting;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Slack.Messages
{
    class AlertV1MessageBuilder : SlackMessageBuilder
    {
        private readonly string _messageTemplate;

        public AlertV1MessageBuilder(Apps.App app, string channel, string username, string messageTemplate, string iconUrl, bool excludeAttachments) 
            : base(app, channel, username, iconUrl, excludeAttachments)
        {
            _messageTemplate = messageTemplate;
        }

        protected override string GenerateMessageText(Event<LogEventData> evt)
        {
            var dashboardUrl = EventFormatting.SafeGetProperty(evt, "DashboardUrl");
            var condition = EventFormatting.SafeGetProperty(evt, "Condition", raw: true);
            var dashboardTitle = EventFormatting.SafeGetProperty(evt, "DashboardTitle");
            var chartTitle = EventFormatting.SafeGetProperty(evt, "ChartTitle");
            var ownerNamespace = "";
            if (evt.Data.Properties.TryGetValue("OwnerUsername", out var ownerUsernameProperty) && ownerUsernameProperty is string ownerUsername)
            {
                if (!string.IsNullOrEmpty(ownerUsername))
                    ownerNamespace = SlackSyntax.Escape(ownerUsername) + "/";
            }
            return $"Alert condition {SlackSyntax.Code(condition)} detected on {SlackSyntax.Hyperlink(dashboardUrl, $"{ownerNamespace}{dashboardTitle}/{chartTitle}")}.";
        }

        protected override void AddAttachments(SlackMessage message, Event<LogEventData> evt, string color)
        {
            var resultsUrl = EventFormatting.SafeGetProperty(evt, "ResultsUrl");
            var resultsText = SlackSyntax.Hyperlink(resultsUrl, "Explore detected results in Seq");
            var results = new SlackMessageAttachment(color, resultsText);
            message.Attachments.Add(results);

            if (_messageTemplate != null)
            {
                message.Attachments.Add(new SlackMessageAttachment(color, _messageTemplate, null, true));
            }
        }
    }
}