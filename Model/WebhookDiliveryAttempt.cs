namespace WebHookSystem.Model
{
    public class WebhookDiliveryAttempt
    {
        public Guid Id { get; set; }

        public Guid WebhookSubscriptionId { get; set; }

        public string Payload {  get; set; }

        public int? ResponseStatusCode { get; set; }

        public bool Success { get; set; }   

        public DateTime TimeStamp { get; set; }
    }
}
