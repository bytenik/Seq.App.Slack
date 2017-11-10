using Newtonsoft.Json;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Seq.App.Slack
{
    [SeqApp("Slack Notifier", Description = "Sends messages matching a view to Slack.")]
    public class SlackReactor : Reactor, ISubscribeTo<LogEventData>
    {
        private static readonly Regex PlaceholdersRegex = new Regex("(\\[(?<key>[^\\[\\]]+?)(\\:(?<format>[^\\[\\]]+?))?\\])", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private const uint AlertEventType = 0xA1E77000;

        [SeqAppSetting(
            DisplayName = "Webhook URL",
            HelpText = "Slack labels this as \"Your Unique Webhook URL\".")]
        public string WebhookUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Channel",
            IsOptional = true,
            HelpText = "The channel to be used for the slack notification.")]
        public string Channel { get; set; }

        [SeqAppSetting(
            DisplayName = "App name",
            IsOptional = true,
            HelpText = "The name that Seq uses when posting to Slack. If not specified, uses the Webhook default. The name can also be read from a property by using the format [PropertyName].")]
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
            HelpText = "The message to send to Slack. Refer to https://api.slack.com/docs/formatting for formatting options. Event property values can be added in the format [PropertyName]. The default is \"[RenderedMessage]\". Ignored for Seq Alerts.",
            IsOptional = true)]
        public string MessageTemplate { get; set; }

        [SeqAppSetting(
            DisplayName = "Icon URL",
            HelpText = "The image to show in the room for the message. The default is https://getseq.net/images/nuget/seq.png",
            IsOptional = true)]
        public string IconUrl { get; set; }

        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly ConcurrentDictionary<uint, DateTime> _lastSeen = new ConcurrentDictionary<uint, DateTime>();

        private static readonly IImmutableDictionary<LogEventLevel, string> LevelToColor = (new Dictionary<LogEventLevel, string> {
            [LogEventLevel.Verbose] = "#D3D3D3",
            [LogEventLevel.Debug] = "#D3D3D3",
            [LogEventLevel.Information] = "#00A000",
            [LogEventLevel.Warning] = "#f9c019",
            [LogEventLevel.Error] = "#e03836",
            [LogEventLevel.Fatal] = "#e03836",
        }).ToImmutableDictionary();

        private static readonly IImmutableList<string> SpecialProperties = ImmutableList.Create("Id", "Host");

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public void On(Event<LogEventData> evt)
        {
            bool added = false;
            var lastSeen = _lastSeen.GetOrAdd(evt.EventType, k => { added = true; return DateTime.UtcNow; });
            if (!added)
            {
                if (lastSeen > DateTime.UtcNow.AddMinutes(-SuppressionMinutes)) return;
                _lastSeen[evt.EventType] = DateTime.UtcNow;
            }

            var color = LevelToColor[evt.Data.Level];

            var message = new
            {
                fallback = "[" + evt.Data.Level + "] " + evt.Data.RenderedMessage,
                text = GenerateMessageText(evt),
                attachments = new ArrayList(),
                username = string.IsNullOrWhiteSpace(Username) ? null : SubstitutePlaceholders(Username, evt, false),
                icon_url = string.IsNullOrWhiteSpace(IconUrl) ? "https://getseq.net/images/nuget/seq.png" : IconUrl,
                channel = Channel,
            };

            if (ExcludePropertyInformation)
            {
                SendMessageToSlack(message);
                return;
            }

            if (IsAlert(evt))
            {
                var resultsUrl = SafeGetProperty(evt, "ResultsUrl");
                var resultsText = $"<{resultsUrl}|Explore detected results in Seq>";
                var results = new
                {
                    color,
                    text = resultsText
                };
                message.attachments.Add(results);

                SendMessageToSlack(message);
                return;
            }

            var special = new
            {
                color = color,
                fields = new ArrayList { new { value = Enum.GetName(typeof(LogEventLevel), evt.Data.Level), title = "Level", @short = true } }
            };
            message.attachments.Add(special);

            foreach (var key in SpecialProperties)
            {
                if (evt.Data.Properties == null || !evt.Data.Properties.ContainsKey(key)) continue;

                var property = evt.Data.Properties[key];
                special.fields.Add(new { value = property.ToString(), title = key, @short = true });
            }

            if (evt.Data.Exception != null)
            {
                message.attachments.Add(new
                {
                    color = color,
                    title = "Exception Details",
                    text = $"```{evt.Data.Exception.Replace("\r", "")}```",
                    mrkdwn_in = new List<string> { "text" },
                });
            }

            if (evt.Data.Properties != null && evt.Data.Properties.ContainsKey("StackTrace"))
            {
                message.attachments.Add(new
                {
                    color = color,
                    title = "Stack Trace",
                    text = $"```{evt.Data.Properties["StackTrace"].ToString().Replace("\r", "")}```",
                    mrkdwn_in = new List<string> { "text" },
                });
            }

            var otherProperties = new
            {
                color = color,
                title = "Properties",
                fields = new ArrayList(),
            };

            if (evt.Data.Properties != null)
            {
                foreach (var property in evt.Data.Properties)
                {
                    if (SpecialProperties.Contains(property.Key)) continue;
                    if (property.Key == "StackTrace") continue;

                    otherProperties.fields.Add(new { value = property.Value?.ToString() ?? "", title = property.Key, @short = false });
                }
            }

            if (otherProperties.fields.Count != 0)
                message.attachments.Add(otherProperties);

            SendMessageToSlack(message);
        }

        private string GenerateMessageText(Event<LogEventData> evt)
        {
            var seqUrl = string.IsNullOrWhiteSpace(BaseUrl) ? Host.ListenUris.FirstOrDefault() : BaseUrl;

            if (IsAlert(evt))
            {
                var dashboardUrl = SafeGetProperty(evt, "DashboardUrl");
                var condition = SafeGetProperty(evt, "Condition", raw: true);
                var dashboardTitle = SafeGetProperty(evt, "DashboardTitle");
                var chartTitle = SafeGetProperty(evt, "ChartTitle");
                return $"Alert condition `{condition}` detected on <{dashboardUrl}|{dashboardTitle}/{chartTitle}>.";
            }

            var messageTemplateToUse = string.IsNullOrWhiteSpace(MessageTemplate) ? "[RenderedMessage]" : MessageTemplate;
            return $"{SubstitutePlaceholders(messageTemplateToUse, evt)} (<{seqUrl}/#/events?filter=@Id%20%3D%3D%20%22{evt.Id}%22&show=expanded|View this event in Seq>)";
        }

        private static bool IsAlert(Event<LogEventData> evt)
        {
            return evt.EventType == AlertEventType;
        }

        private static string SafeGetProperty(Event<LogEventData> evt, string propertyName, bool raw = false)
        {
            if (evt.Data.Properties.TryGetValue(propertyName, out var value))
            {
                if (value == null) return "`null`";
                return raw ? value.ToString() : SlackMessage.Escape(value.ToString());
            }
            return "";
        }

        private void SendMessageToSlack(object message)
        {
            var json = JsonConvert.SerializeObject(message, JsonSerializerSettings);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var resp = HttpClient.PostAsync(WebhookUrl, content).Result;
                resp.EnsureSuccessStatusCode();
            }
        }

        private string SubstitutePlaceholders(string messageTemplateToUse, Event<LogEventData> evt, bool addLogData = true)
        {
            var data = evt.Data;
            var eventType = evt.EventType;
            var level = data.Level;

            var placeholders = data.Properties?.ToDictionary(k => k.Key.ToLower(), v => v.Value) ?? new Dictionary<string, object>();

            if (addLogData)
            {
                AddValueIfKeyDoesntExist(placeholders, "Level", level);
                AddValueIfKeyDoesntExist(placeholders, "EventType", eventType);
                AddValueIfKeyDoesntExist(placeholders, "RenderedMessage", data.RenderedMessage);
            }
            return PlaceholdersRegex.Replace(messageTemplateToUse, m =>
            {
                var key = m.Groups["key"].Value.ToLower();
                var format = m.Groups["format"].Value;
                return placeholders.ContainsKey(key) ? FormatValue(placeholders[key], format) : m.Value;
            });
        }

        private string FormatValue(object value, string format)
        {
            var rawValue = value?.ToString() ?? "`null`";

            if (string.IsNullOrWhiteSpace(format))
                return rawValue;

            try
            {
                return string.Format(format, rawValue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not format Slack message: {Value} {Format}", value, format);
            }

            return rawValue;
        }

        private static void AddValueIfKeyDoesntExist(IDictionary<string, object> placeholders, string key, object value)
        {
            var loweredKey = key.ToLower();
            if (!placeholders.ContainsKey(loweredKey))
                placeholders.Add(loweredKey, value);
        }
    }
}
