using Xunit;

namespace Seq.App.Slack.Tests
{
    public class SlackSyntaxTests
    {
        [Fact]
        public void HyperlinksAreCorrectlyFormatted()
        {
            var link = SlackSyntax.Hyperlink("http://example.com", "Hello, world!");
            Assert.Equal("<http://example.com|Hello, world!>", link);
        }

    }

}
