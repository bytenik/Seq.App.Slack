using System.Collections.Generic;
using Seq.App.Slack.Formatting;
using Xunit;

namespace Seq.App.Slack.Tests
{
    public class PropertyValueFormatterTests
    {
        [Fact]
        public void IntPropertyFormattedOk()
        {
            var formatter = new PropertyValueFormatter(null);
            var result = formatter.ConvertPropertyValueToString(1);
            Assert.Equal("1", result);
        }

        [Fact]
        public void NullPropertyFormattedOk()
        {
            var formatter = new PropertyValueFormatter(null);
            var result = formatter.ConvertPropertyValueToString(null);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void DictionaryPropertiesSerialisedAsJson()
        {
            var formatter = new PropertyValueFormatter(null);

            var d = new Dictionary<string, string>
            {
                { "test", "value" },
                { "test2", "value2" }
            };
            var result = formatter.ConvertPropertyValueToString(d);
            const string expected = "{\"test\":\"value\",\"test2\":\"value2\"}";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void PropertiesShouldTruncateIfDesired()
        {
            var formatter = new PropertyValueFormatter(5);

            var d = new Dictionary<string, string> { { "test", "value" } };
            var result = formatter.ConvertPropertyValueToString(d);
            const string expected = "{\"tes...";
            Assert.Equal(expected, result);
        }

    }

}
