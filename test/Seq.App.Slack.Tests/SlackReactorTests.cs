using NSubstitute;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Seq.App.Slack.Tests
{
    public class SlackReactorTests
    {
        private SlackReactor _slackReactor;
        private ISlackApi _slackApi;
        private IAppHost _appHost;
        private Event<LogEventData> _event;

        public SlackReactorTests()
        {
            _slackApi = Substitute.For<ISlackApi>();
            _appHost = Substitute.For<IAppHost>();
            _appHost.Host.Returns(new Host(new[] { "listenUri" }, "instance"));
            _appHost.App.Returns(new Apps.App("app-id", "App Title", new Dictionary<string, string>(), "storage-path"));

            _slackReactor = new SlackReactor(_slackApi)
            {
                WebhookUrl = "http://webhookurl.com"
            };
            _slackReactor.Attach(_appHost);

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
        public void GivenIncludedPropertiesAreSupplieThenTheyAreRespected()
        {
            _slackReactor.IncludedProperties = "Property1,Property3";             

            _slackReactor.On(_event);

            // Ensure the message we send to slack only includes the properties specified
            _slackApi.Received().SendMessage(_slackReactor.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 2 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property3")));
        }

        [Fact]
        public void GivenIncludedPropertiesNotSetThenAllPropertiesAreIncluded()
        {
            _slackReactor.On(_event);

            // Ensure the message we send to slack only includes the properties specified
            _slackApi.Received().SendMessage(_slackReactor.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 3 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property2") &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property3")));
        }

        [Fact]
        public void GivenIncludedPropertiesContainsPropertiesThatDontExistThenTheyAreIgnored()
        {
            _slackReactor.IncludedProperties = "Property1,PropertyDoesntExist";              

            _slackReactor.On(_event);

            // Ensure the message we send to slack only includes the properties specified
            _slackApi.Received().SendMessage(_slackReactor.WebhookUrl, Arg.Is<SlackMessage>(x => x.Attachments.Single(a => a.Text == "Properties").Fields.Count == 1 &&
                                                                                               x.Attachments.Single(a => a.Text == "Properties").Fields.Any(a => a.Title == "Property1")));
        }
    }
}
