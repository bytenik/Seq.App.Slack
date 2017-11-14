namespace Seq.App.Slack
{
    static class SlackMessage
    {
        public static string Escape(string s)
        {
            return s
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("&", "&amp;");
        }
    }
}
