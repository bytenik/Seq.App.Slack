using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Seq.App.Slack
{
    static class SlackApi
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public static void SendMessage(string webhookUrl, SlackMessage message)
        {
            var json = JsonConvert.SerializeObject(message, JsonSerializerSettings);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var resp = HttpClient.PostAsync(webhookUrl, content).Result;
                resp.EnsureSuccessStatusCode();
            }
        }
    }
}
