using System.Collections.Generic;
using Xunit;

namespace Seq.App.Slack.Tests
{
    public class PropertyFormatterTests
    {
        SlackApp _slackApp = new SlackApp();

        [Fact]
        public void IntPropertyFormattedOk()
        {
            var result = _slackApp.ConvertPropertyValueToString(1);
            Assert.Equal("1", result);
        }

        [Fact]
        public void NullPropertyFormattedOk()
        {
            var result = _slackApp.ConvertPropertyValueToString(null);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void DictionaryPropertiesSerialisedAsJson()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("test", "value");
            d.Add("test2", "value2");
            var result = _slackApp.ConvertPropertyValueToString(d);
            var expected = "{\"test\":\"value\",\"test2\":\"value2\"}";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void PropertiesShouldTruncateIfDesired()
        {
            _slackApp.MaxPropertyLength = 5;

            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("test", "value");
            var result = _slackApp.ConvertPropertyValueToString(d);
            var expected = "{\"tes...";
            Assert.Equal(expected, result);
        }

    }

}
