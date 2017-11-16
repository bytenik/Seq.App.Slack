using Newtonsoft.Json;

namespace Seq.App.Slack
{
    public class SlackMessageAttachmentField
    {
        [JsonProperty("title")]
        public string Title { get; }

        [JsonProperty("value")]
        public string Value { get; }

        [JsonProperty("short")]
        public bool Short { get; }

        public SlackMessageAttachmentField(string title, string value, bool @short)
        {
            Title = title;
            Value = value;
            Short = @short;
        }
    }
}