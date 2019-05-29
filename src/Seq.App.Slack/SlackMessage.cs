using System.Collections.Generic;
using Newtonsoft.Json;

namespace Seq.App.Slack
{
    public class SlackMessage
    {
        [JsonProperty("fallback")]
        public string Fallback { get; }

        [JsonProperty("text")]
        public string Text { get; }

        [JsonProperty("attachments")]
        public List<SlackMessageAttachment> Attachments { get; }

        [JsonProperty("username")]
        public string Username { get; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; }

        [JsonProperty("channel")]
        public string Channel { get; }

        public SlackMessage(string fallback, string text, string username, string iconUrl, string channel)
        {
            this.Fallback = fallback;
            this.Text = text;
            this.Attachments = new List<SlackMessageAttachment>();
            this.Username = username;
            this.IconUrl = iconUrl;
            this.Channel = channel;
        }
    }
}