using System.Threading.Tasks;

namespace Seq.App.Slack
{
    public interface ISlackApi
    {
        Task SendMessageAsync(string webhookUrl, SlackMessage message);
    }
}
