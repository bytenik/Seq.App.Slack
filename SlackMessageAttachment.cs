using System.Collections.Generic;
using Newtonsoft.Json;

namespace Seq.App.Slack
{
    public class SlackMessageAttachment
    {
        [JsonProperty("color")]
        public string Color { get; }

        [JsonProperty("text")]
        public string Text { get; }

        [JsonProperty("title")]
        public string Title { get; }

        [JsonProperty("fields")]
        public List<SlackMessageAttachmentField> Fields { get; }

        [JsonProperty("mrkdwn_in")]
        public List<string> MarkdownIn { get; }

        public SlackMessageAttachment(string color, string text = null, string title = null, bool textIsMarkdown = false)
        {
            this.Color = color;
            this.Text = text;
            Title = title;
            this.Fields = new List<SlackMessageAttachmentField>();
            this.MarkdownIn = new List<string>();

            if (textIsMarkdown)
                 this.MarkdownIn.Add("text");
        }
    }
}
