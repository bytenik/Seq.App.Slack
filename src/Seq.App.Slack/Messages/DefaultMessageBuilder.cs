using System;
using System.Collections.Generic;
using System.Linq;
using Seq.App.Slack.Api;
using Seq.App.Slack.Formatting;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Slack.Messages
{
    class DefaultMessageBuilder : SlackMessageBuilder
    {
        private readonly Host _host;
        private readonly PropertyValueFormatter _propertyValueFormatter;
        private readonly string _messageTemplate;
        private readonly HashSet<string> _includedProperties;
        
        private static readonly IEnumerable<string> SpecialProperties = new[] { "Id", "Host" };

        public DefaultMessageBuilder(Host host, Apps.App app, PropertyValueFormatter propertyValueFormatter, string channel,
            string username, string iconUrl, string messageTemplate, bool excludeOptionalAttachments, IEnumerable<string> includedProperties)
            : base(app, channel, username, iconUrl, excludeOptionalAttachments)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _propertyValueFormatter = propertyValueFormatter ?? throw new ArgumentNullException(nameof(propertyValueFormatter));
            _messageTemplate = messageTemplate;
            _includedProperties = new HashSet<string>(includedProperties ?? throw new ArgumentNullException(nameof(includedProperties)));
        }

        protected override string GenerateMessageText(Event<LogEventData> evt)
        {
            var messageTemplateToUse = string.IsNullOrWhiteSpace(_messageTemplate) ? "[RenderedMessage]" : _messageTemplate;
            var message = EventFormatting.SubstitutePlaceholders(messageTemplateToUse, evt);
            return SlackSyntax.Escape(message);
        }

        protected override void AddNecessaryAttachments(SlackMessage message, Event<LogEventData> evt, string color)
        {
            var viewUrl = EventFormatting.LinkToId(_host, evt.Id);
            var viewText = SlackSyntax.Hyperlink(viewUrl, "View this event in Seq");
            var view = new SlackMessageAttachment(color, viewText);
            message.Attachments.Add(view);
        }

        protected override void AddOptionalAttachments(SlackMessage message, Event<LogEventData> evt, string color)
        {
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
                    if (_includedProperties.Any() && !_includedProperties.Contains(property.Key)) continue;

                    var value = _propertyValueFormatter.ConvertPropertyValueToString(property.Value);
                    
                    otherProperties.Fields.Add(new SlackMessageAttachmentField(property.Key, value, @short: false));
                }
            }

            if (otherProperties.Fields.Count != 0)
                message.Attachments.Add(otherProperties);
        }
    }
}