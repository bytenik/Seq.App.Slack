using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Seq.App.Slack
{
    public interface ISlackApi
    {
        void SendMessage(string webhookUrl, SlackMessage message);
    }

    class SlackApi : ISlackApi
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public SlackApi(string proxyServer)
        {
            if (!string.IsNullOrWhiteSpace(proxyServer))
            {
                var proxy = new WebProxy(proxyServer, false)
                {
                    UseDefaultCredentials = true
                };
                var httpClientHandler = new HttpClientHandler()
                {
                    Proxy = proxy,
                    PreAuthenticate = true,
                    UseDefaultCredentials = true,
                };
                _httpClient = new HttpClient(handler: httpClientHandler);
            }
            else
            {
                _httpClient = new HttpClient();
            }
        }

        
        
        public void SendMessage(string webhookUrl, SlackMessage message)
        {
            var json = JsonConvert.SerializeObject(message, JsonSettings);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var resp = _httpClient.PostAsync(webhookUrl, content).Result;
                resp.EnsureSuccessStatusCode();
            }
        }
    }
}
