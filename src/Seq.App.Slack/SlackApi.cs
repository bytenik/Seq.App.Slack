using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Seq.App.Slack
{
    class SlackApi
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public SlackApi(string proxyServer)
        {
            if (!string.IsNullOrWhiteSpace(proxyServer))
            {
                WebProxy proxy = new WebProxy(proxyServer, false)
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
            var json = JsonConvert.SerializeObject(message, _jsonSettings);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var resp = _httpClient.PostAsync(webhookUrl, content).Result;
                resp.EnsureSuccessStatusCode();
            }
        }
    }
}
