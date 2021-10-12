using System.Threading.Tasks;

namespace Seq.App.Slack.Api
{
    public interface ISlackApi
    {
        Task SendMessageAsync(string webhookUrl, SlackMessage message);
    }
}
