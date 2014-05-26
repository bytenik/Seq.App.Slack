using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Seq.Slack
{
    [SeqApp("Slack Notifier", Description = "Sends messages matching a view to Slack.")]
    public class SlackReactor : Reactor, ISubscribeTo<LogEventData>
    {
        [SeqAppSetting(
            DisplayName = "Webhook URL",
            HelpText = "Slack labels this as \"Your Unique Webhook URL\".")]
        public string WebhookUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Suppression time (minutes)",
            IsOptional = true,
            HelpText = "Once an event type has been sent to Slack, the time to wait before sending again. The default is zero.")]
        public int SuppressionMinutes { get; set; } = 0;

        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly ConcurrentDictionary<uint, DateTime> _lastSeen = new ConcurrentDictionary<uint, DateTime>();

        private static readonly ImmutableDictionary<LogEventLevel, string> LevelToColor = (new Dictionary<LogEventLevel, string> {
            [LogEventLevel.Verbose] = "#D3D3D3",
            [LogEventLevel.Debug] = "#D3D3D3",
            [LogEventLevel.Information] = "#00A000",
            [LogEventLevel.Warning] = "#f9c019",
            [LogEventLevel.Error] = "#e03836",
            [LogEventLevel.Fatal] = "#e03836",
        }).ToImmutableDictionary();

        private static readonly ImmutableList<string> SpecialProperties = ImmutableList.Create("Id", "Host");

        public void On(Event<LogEventData> evt)
        {
            bool added = false;
            var lastSeen = _lastSeen.GetOrAdd(evt.EventType, k => { added = true; return DateTime.UtcNow; });
            if (!added && lastSeen > DateTime.UtcNow.AddMinutes(-SuppressionMinutes))
                return;

            var color = LevelToColor[evt.Data.Level];

            var message = new
            {
                fallback = "[" + evt.Data.Level + "] " + evt.Data.RenderedMessage,
                text = evt.Data.RenderedMessage,
                attachments = new JArray()
            };

            var special = new
            {
                color = color,
                values = new JArray { new { title = "Level", value = Enum.GetName(typeof(LogEventLevel), evt.Data.Level) } }
            };
            message.attachments.Add(special);
            foreach (var key in SpecialProperties)
            {
                if (evt.Data.Properties.ContainsKey(key))
                {
                    var property = evt.Data.Properties[key];
                    special.values.Add(new { value = property.ToString(), title = key, @short = true });
                }
            }

            if (evt.Data.Exception != null)
            {
                message.attachments.Add(new
                {
                    color = color,
                    title = "Exception Details",
                    text = "```" + evt.Data.Exception.Replace("\r", "").Replace("\n", @"\n") + "```",
                    mrkdwn_in = new JArray { "text" },
                });
            }

            var otherProperties = new
            {
                color = color,
                title = "Properties",
                values = new JArray(),
            };
            foreach (var property in evt.Data.Properties)
                otherProperties.values.Add(new { value = property.Value.ToString(), title = property.Key, @short = false });
            if (otherProperties.values.Count != 0)
                message.attachments.Add(otherProperties);

            var json = JsonConvert.SerializeObject(message);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var resp = HttpClient.PostAsync(WebhookUrl, content).Result;
                resp.EnsureSuccessStatusCode();
            }
        }
    }
}
