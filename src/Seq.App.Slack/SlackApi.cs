using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Seq.App.Slack
{
    class SlackApi
    {
        private HttpClient httpClient;

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
                httpClient = new HttpClient(handler: httpClientHandler);
            }
            else
            {
                httpClient = new HttpClient();
            }
        }

        
        
        public void SendMessage(string webhookUrl, SlackMessage message)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(message, settings);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var resp = httpClient.PostAsync(webhookUrl, content).Result;
                resp.EnsureSuccessStatusCode();
            }
        }
    }
}
