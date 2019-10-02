namespace Seq.App.Slack
{
    public interface ISlackApi
    {
        void SendMessage(string webhookUrl, SlackMessage message);
    }
}
