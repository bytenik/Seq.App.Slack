using System;

namespace Seq.App.Slack.Formatting
{
    static class SlackSyntax
    {
        public static string Escape(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            return s
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("&", "&amp;");
        }

        public static string Hyperlink(string url, string caption)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            if (caption == null) throw new ArgumentNullException(nameof(caption));
            return $"<{url}|{caption}>";
        }

        public static string Preformatted(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            return $"```\n{s.Replace("\r", "")}\n```";
        }

        public static string Code(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            return $"`{s}`";
        }
    }
}
