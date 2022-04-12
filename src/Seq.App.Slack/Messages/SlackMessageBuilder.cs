using System;
using Seq.App.Slack.Api;
using Seq.App.Slack.Formatting;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Slack.Messages
{
    abstract class SlackMessageBuilder
    {
        public const string DefaultIconUrl = "https://datalust.co/images/nuget/seq-apps.png";

        private readonly Apps.App _app;
        private readonly string _channel;
        private readonly string _username;
        private readonly string _iconUrl;
        private readonly bool _excludeOptionalAttachments;

        protected SlackMessageBuilder(Apps.App app, string channel,
            string username, string iconUrl, bool excludeOptionalAttachments)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _channel = channel;
            _username = username;
            _iconUrl = iconUrl;
            _excludeOptionalAttachments = excludeOptionalAttachments;
        }

        public SlackMessage BuildMessage(Event<LogEventData> evt)
        {
            var message = new SlackMessage("[" + evt.Data.Level + "] " + evt.Data.RenderedMessage,
                GenerateMessageText(evt),
                string.IsNullOrWhiteSpace(_username)
                    ? _app.Title
                    : EventFormatting.SubstitutePlaceholders(_username, evt, false),
                string.IsNullOrWhiteSpace(_iconUrl) ? DefaultIconUrl : _iconUrl,
                _channel);

            var color = EventFormatting.LevelToColor(evt.Data.Level);
            AddNecessaryAttachments(message, evt, color);
            
            if (!_excludeOptionalAttachments)
            {
                AddOptionalAttachments(message, evt, color);
            }

            return message;
        }

        protected abstract string GenerateMessageText(Event<LogEventData> evt);

        /// <summary>
        /// Add attachments without which the message cannot be reliably interpreted by a user.
        /// </summary>
        protected virtual void AddNecessaryAttachments(SlackMessage message, Event<LogEventData> evt, string color)
        {
        }

        /// <summary>
        /// Add attachments that don't impact the meaning/interpretation of the message.
        /// </summary>
        protected virtual void AddOptionalAttachments(SlackMessage message, Event<LogEventData> evt, string color)
        {
        }
    }
}
