using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Seq.App.Slack
{
    [SeqApp("Slack Notifier", Description = "Sends events to a Slack channel.")]
    public class SlackReactor : Reactor, ISubscribeTo<LogEventData>
    {
        private const uint AlertEventType = 0xA1E77000;
        private const string DefaultIconUrl = "https://getseq.net/images/nuget/seq-apps.png";

        [SeqAppSetting(
            DisplayName = "Webhook URL",
            HelpText = "Add the Incoming WebHooks app to your Slack to get this URL.")]
        public string WebhookUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Channel",
            IsOptional = true,
            HelpText = "The channel to be used for the Slack notification. If not specified, uses the webhook default.")]
        public string Channel { get; set; }

        [SeqAppSetting(
            DisplayName = "App name",
            IsOptional = true,
            HelpText = "The name that Seq uses when posting to Slack. If not specified, uses the name of the Seq app instance. The name can also be read from a property by using the format [PropertyName].")]
        public string Username { get; set; }

        [SeqAppSetting(
            DisplayName = "Seq base URL",
            HelpText = "Used for generating links to events in Slack messages. The default is the value supplied by Seq.",
            IsOptional = true)]
        public string BaseUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Suppression time (minutes)",
            IsOptional = true,
            HelpText = "Once an event type has been sent to Slack, the time to wait before sending again. The default is zero.")]
        public int SuppressionMinutes { get; set; } = 0;

        [SeqAppSetting(
            DisplayName = "Exclude properties",
            IsOptional = true,
            HelpText = "Should the event include the property information as attachments to the message. The default is to attach all properties.")]
        public bool ExcludePropertyInformation { get; set; }

        [SeqAppSetting(
            DisplayName = "Message",
            HelpText = "The message to send to Slack. Refer to https://api.slack.com/docs/formatting for formatting options. Event property values can be added in the format [PropertyName]. The default is \"[RenderedMessage]\". Adds a markdown as attachment:text for Alerts.",
            IsOptional = true)]
        public string MessageTemplate { get; set; }

        [SeqAppSetting(
            DisplayName = "Icon URL",
            HelpText = "The image to show in the room for the message. The default is https://getseq.net/images/nuget/seq-apps.png",
            IsOptional = true)]
        public string IconUrl { get; set; }

        private EventTypeSuppressions _suppressions;

        private static readonly IImmutableList<string> SpecialProperties = ImmutableList.Create("Id", "Host");

        public void On(Event<LogEventData> evt)
        {
            _suppressions = _suppressions ?? new EventTypeSuppressions(SuppressionMinutes);
            if (_suppressions.ShouldSuppressAt(evt.EventType, DateTime.UtcNow))
                return;

            var color = EventFormatting.LevelToColor(evt.Data.Level);

            var message = new SlackMessage("[" + evt.Data.Level + "] " + evt.Data.RenderedMessage,
                                           GenerateMessageText(evt),
                                           string.IsNullOrWhiteSpace(Username) ? App.Title : EventFormatting.SubstitutePlaceholders(Username, evt, false),
                                           string.IsNullOrWhiteSpace(IconUrl) ? DefaultIconUrl : IconUrl,
                                           Channel);

            if (ExcludePropertyInformation)
            {
                SlackApi.SendMessage(WebhookUrl, message);
                return;
            }

            if (IsAlert(evt))
            {
                var resultsUrl = EventFormatting.SafeGetProperty(evt, "ResultsUrl");
                var resultsText = SlackSyntax.Hyperlink(resultsUrl, "Explore detected results in Seq");
                var results = new SlackMessageAttachment(color, resultsText);
                message.Attachments.Add(results);

                if(MessageTemplate != null)
                {
                    message.Attachments.Add(new SlackMessageAttachment(color, MessageTemplate, null, true));
                }

                SlackApi.SendMessage(WebhookUrl, message);
                return;
            }

            var special = new SlackMessageAttachment(color);
            special.Fields.Add(new SlackMessageAttachmentField("Level", evt.Data.Level.ToString(), @short: true));
            message.Attachments.Add(special);

            foreach (var key in SpecialProperties)
            {
                if (evt.Data.Properties == null || !evt.Data.Properties.ContainsKey(key)) continue;

                var property = evt.Data.Properties[key];
                special.Fields.Add(new SlackMessageAttachmentField(key, property.ToString(), @short: true ));
            }

            if (evt.Data.Exception != null)
            {
                message.Attachments.Add(new SlackMessageAttachment(color, SlackSyntax.Preformatted(evt.Data.Exception), "Exception Details", textIsMarkdown: true));
            }

            if (evt.Data.Properties != null && evt.Data.Properties.TryGetValue("StackTrace", out var st) && st is string stackTrace)
            {
                message.Attachments.Add(new SlackMessageAttachment(color, SlackSyntax.Preformatted(stackTrace), "Stack Trace", textIsMarkdown: true));
            }

            var otherProperties = new SlackMessageAttachment(color, "Properties");
            if (evt.Data.Properties != null)
            {
                foreach (var property in evt.Data.Properties)
                {
                    if (SpecialProperties.Contains(property.Key)) continue;
                    if (property.Key == "StackTrace") continue;

                    otherProperties.Fields.Add(new SlackMessageAttachmentField(property.Key, property.Value?.ToString() ?? "", @short: false));
                }
            }

            if (otherProperties.Fields.Count != 0)
                message.Attachments.Add(otherProperties);

            SlackApi.SendMessage(WebhookUrl, message);
        }

        private string GenerateMessageText(Event<LogEventData> evt)
        {
            var seqUrl = string.IsNullOrWhiteSpace(BaseUrl) ? Host.ListenUris.FirstOrDefault() : BaseUrl;

            if (IsAlert(evt))
            {
                var dashboardUrl = EventFormatting.SafeGetProperty(evt, "DashboardUrl");
                var condition = EventFormatting.SafeGetProperty(evt, "Condition", raw: true);
                var dashboardTitle = EventFormatting.SafeGetProperty(evt, "DashboardTitle");
                var chartTitle = EventFormatting.SafeGetProperty(evt, "ChartTitle");
                return $"Alert condition {SlackSyntax.Code(condition)} detected on {SlackSyntax.Hyperlink(dashboardUrl, $"{dashboardTitle}/{chartTitle}")}.";
            }

            var messageTemplateToUse = string.IsNullOrWhiteSpace(MessageTemplate) ? "[RenderedMessage]" : MessageTemplate;
            var message = EventFormatting.SubstitutePlaceholders(messageTemplateToUse, evt);
            var link = SlackSyntax.Hyperlink($"{seqUrl?.TrimEnd('/')}/#/events?filter=@Id%20%3D%3D%20%22{evt.Id}%22&show=expanded", "View this event in Seq");
            return $"{message} ({link})";
        }

        private static bool IsAlert(Event<LogEventData> evt)
        {
            return evt.EventType == AlertEventType;
        }
    }
}
