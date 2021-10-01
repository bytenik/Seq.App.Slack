using System;
using System.Collections.Generic;
using Seq.App.Slack.Formatting;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Xunit;

namespace Seq.App.Slack.Tests
{
    public class EventFormattingTests
    {
        [Fact]
        public void SubstitutePlaceholders_ReplacesValue()
        {
            var result = ExecuteSubstitutePlaceholders(new Dictionary<string, object>()
            {
                {"noun", "force"},
                {"name", "Luke"}
            });

            Assert.Equal("Use the force Luke", result);
        }
        
        [Fact]
        public void SubstitutePlaceholders_IgnoresCase()
        {
            var result = ExecuteSubstitutePlaceholders(new Dictionary<string, object>()
            {
                {"Noun", "force"},
                {"naMe", "Newton"}
            });

            Assert.Equal("Use the force Newton", result);
        }

        [Fact]
        public void SubstitutePlaceholders_IgnoresMissingProperties()
        {
            var result = ExecuteSubstitutePlaceholders(new Dictionary<string, object>()
            {
                {"noun", "spoon"}
            });

            Assert.Equal("Use the spoon [name]", result);
        }
        
        [Fact]
        public void SubstitutePlaceholders_AllowsPropertiesThatOnlyDifferByCase()
        {
            var result = ExecuteSubstitutePlaceholders(new Dictionary<string, object>()
            {
                {"noun", "velcro"},
                {"Noun", "zipper"}
            });

            Assert.Equal("Use the zipper [name]", result);
        }
        
        [Fact]
        public void SubstitutePlaceholders_SkipsSubstitutionIfPropertiesIsNull()
        {
            var result = ExecuteSubstitutePlaceholders(null);

            Assert.Equal("Use the [noun] [name]", result);
        }

        [Theory]
        [InlineData("First", "`null`")]
        [InlineData("Second", "20")]
        [InlineData("Third", "System.Collections.Generic.Dictionary`2[System.String,System.Object]")]
        [InlineData("Third.Fourth", "test")]
        [InlineData("Fifth", "")]
        public void PropertiesAreRetrievedSafely(string path, string expected)
        {
            var data = new LogEventData
            {
                Properties = new Dictionary<string, object>
                {
                    ["First"] = null,
                    ["Second"] = 20,
                    ["Third"] = new Dictionary<string, object>
                    {
                        ["Fourth"] = "test"
                    }
                }
            };
            
            var evt = new Event<LogEventData>("event-123", 4, DateTime.UtcNow, data);

            var actual = EventFormatting.SafeGetProperty(evt, path);
            Assert.Equal(expected, actual);
        }

        private static string ExecuteSubstitutePlaceholders(IReadOnlyDictionary<string, object> properties)
            => EventFormatting.SubstitutePlaceholders(
                "Use the [noun] [name]",
                new Event<LogEventData>("", 1, DateTime.Now, new LogEventData() {Properties = properties})
            );
    }
}
