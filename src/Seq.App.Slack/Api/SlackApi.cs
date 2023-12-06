using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Seq.App.Slack.Api
{
    class SlackApi : ISlackApi
    {
        static SlackApi()
        {
            // Enable TLS 1.2 before any connection to the Slack API is made.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public SlackApi(string proxyServer, string hostHeader)
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

            if (!string.IsNullOrWhiteSpace(hostHeader))
                _httpClient.DefaultRequestHeaders.Host = new Uri(hostHeader).Host;
        }

        public async Task SendMessageAsync(string webhookUrl, SlackMessage message)
        {
            var json = JsonConvert.SerializeObject(message, JsonSettings);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var resp = await _httpClient.PostAsync(webhookUrl, content);
                resp.EnsureSuccessStatusCode();
            }
        }
    }
}
