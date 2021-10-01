using NSubstitute;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Seq.App.Slack.Api;
using Xunit;

namespace Seq.App.Slack.Tests
{
    public class SlackAppTests
    {
        private ISlackApi _slackApi;
        private IAppHost _appHost;
        private Event<LogEventData> _event;

        private SlackApp CreateSlackApp(Action<SlackApp> configure = null)
        {
            _slackApi = Substitute.For<ISlackApi>();
            _appHost = Substitute.For<IAppHost>();
            _appHost.Host.Returns(new Host("http://listen.example.com", "instance"));
            _appHost.App.Returns(new Apps.App("app-id", "App Title", new Dictionary<string, string>(), "storage-path"));

            var slackApp = new SlackApp(_slackApi)
            {
                WebhookUrl = "http://webhook.example.com"
            };
            
            configure?.Invoke(slackApp);
            
            slackApp.Attach(_appHost);

            _event = new Event<LogEventData>("id", 1, DateTime.Now, new LogEventData
            {
                Id = "111",
                Level = LogEventLevel.Information,
                Properties = new Dictionary<string, object>
                {
                    {"Property1", "Value1"},
                    {"Property2", "Value2"},
                    {"Property3", "Value3"}
                }
            });

            return slackApp;
        }

        [Fact]
        public async Task GivenIncludedPropertiesWithWhitespaceAreSuppliedThenTheyAreRespected()
        {
            var slackApp = CreateSlackApp(app => app.IncludedProperties = "  Property1 ,   Property2  ");             

            await slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 2 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property2")));
        }

        [Fact]
        public async Task GivenIncludedPropertiesAreSuppliedThenTheyAreRespected()
        {
            var slackApp = CreateSlackApp(app => app.IncludedProperties = "Property1,Property3");

            await slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 2 &&
                                                                                                 x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                                 x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property3")));
        }

        [Fact]
        public async Task GivenIncludedPropertiesNotSetThenAllPropertiesAreIncluded()
        {
            var slackApp = CreateSlackApp();
            await slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 3 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property2") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property3")));
        }

        [Fact]
        public async Task GivenIncludedPropertiesContainsPropertiesThatDontExistThenTheyAreIgnored()
        {
            var slackApp = CreateSlackApp(app => app.IncludedProperties = "Property1,PropertyDoesntExist");

            await slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 1 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1")));
        }
    }
}
