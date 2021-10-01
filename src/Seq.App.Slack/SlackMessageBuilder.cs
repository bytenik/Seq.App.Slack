using System;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Slack
{
    abstract class SlackMessageBuilder
    {
        public const string DefaultIconUrl = "https://datalust.co/images/nuget/seq-apps.png";

        private readonly Apps.App _app;
        private readonly string _channel;
        private readonly string _username;
        private readonly string _iconUrl;
        private readonly bool _excludeAttachments;

        protected SlackMessageBuilder(Apps.App app, string channel,
            string username, string iconUrl, bool excludeAttachments)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _channel = channel;
            _username = username;
            _iconUrl = iconUrl;
            _excludeAttachments = excludeAttachments;
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

            if (!_excludeAttachments)
            {
                var color = EventFormatting.LevelToColor(evt.Data.Level);
                AddAttachments(message, evt, color);
            }

            return message;
        }

        protected abstract string GenerateMessageText(Event<LogEventData> evt);

        protected abstract void AddAttachments(SlackMessage message, Event<LogEventData> evt, string color);
    }
}
