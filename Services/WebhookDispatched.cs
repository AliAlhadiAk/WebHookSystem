namespace WebHookSystem.Services
{
    internal sealed record WebhookDispatched(string EventType, object Data);

}
