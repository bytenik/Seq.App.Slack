using NSubstitute;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Seq.App.Slack.Tests
{
    public class SlackReactorTests
    {
        private SlackApp _slackApp;
        private ISlackApi _slackApi;
        private IAppHost _appHost;
        private Event<LogEventData> _event;

        public SlackReactorTests()
        {
            _slackApi = Substitute.For<ISlackApi>();
            _appHost = Substitute.For<IAppHost>();
            _appHost.Host.Returns(new Host(new[] { "listenUri" }, "instance"));
            _appHost.App.Returns(new Apps.App("app-id", "App Title", new Dictionary<string, string>(), "storage-path"));

            _slackApp = new SlackApp(_slackApi)
            {
                WebhookUrl = "http://webhookurl.com"
            };
            _slackApp.Attach(_appHost);

            _event = new Event<LogEventData>("id", 1, DateTime.Now, new LogEventData
            {
                Id = "111",
                Level = LogEventLevel.Information,
                Properties = new Dictionary<string, object>()
                {
                    {"Property1", "Value1"},
                    {"Property2", "Value2"},
                    {"Property3", "Value3"}
                }
            });
        }

        [Fact]
        public async Task GivenIncludedPropertiesWithWhitespaceAreSuppliedThenTheyAreRespected()
        {
            _slackApp.IncludedProperties = "  Property1 ,   Property2  ";             

            await _slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(_slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 2 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property2")));
        }

        [Fact]
        public async Task GivenIncludedPropertiesAreSuppliedThenTheyAreRespected()
        {
            _slackApp.IncludedProperties = "Property1,Property3";

            await _slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(_slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 2 &&
                                                                                                 x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                                 x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property3")));
        }

        [Fact]
        public async Task GivenIncludedPropertiesNotSetThenAllPropertiesAreIncluded()
        {
            await _slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(_slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 3 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property2") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property3")));
        }

        [Fact]
        public async Task GivenIncludedPropertiesContainsPropertiesThatDontExistThenTheyAreIgnored()
        {
            _slackApp.IncludedProperties = "Property1,PropertyDoesntExist";              

            await _slackApp.OnAsync(_event);

            // Ensure the message we send to slack only includes the properties specified
            await _slackApi.Received().SendMessageAsync(_slackApp.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 1 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1")));
        }
    }
}
