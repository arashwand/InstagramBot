namespace InstagramBot.Application.DTOs
{
    public class WebhookVerificationDto
    {
        public string Mode { get; set; }
        public string Challenge { get; set; }
        public string VerifyToken { get; set; }
    }

    public class InstagramWebhookDto
    {
        public string Object { get; set; }
        public List<InstagramWebhookEntry> Entry { get; set; }
    }

    public class InstagramWebhookEntry
    {
        public string Id { get; set; }
        public long Time { get; set; }
        public List<InstagramWebhookChange> Changes { get; set; }
        public List<InstagramWebhookMessaging> Messaging { get; set; }
    }

    public class InstagramWebhookChange
    {
        public string Field { get; set; }
        public InstagramWebhookValue Value { get; set; }
    }

    public class InstagramWebhookValue
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string MediaId { get; set; }
        public InstagramWebhookFrom From { get; set; }
        public InstagramWebhookMedia Media { get; set; }
    }

    public class InstagramWebhookFrom
    {
        public string Id { get; set; }
        public string Username { get; set; }
    }

    public class InstagramWebhookMedia
    {
        public string Id { get; set; }
        public string MediaProductType { get; set; }
    }

    public class InstagramWebhookMessaging
    {
        public InstagramWebhookSender Sender { get; set; }
        public InstagramWebhookRecipient Recipient { get; set; }
        public long Timestamp { get; set; }
        public InstagramWebhookMessage Message { get; set; }
    }

    public class InstagramWebhookSender
    {
        public string Id { get; set; }
    }

    public class InstagramWebhookRecipient
    {
        public string Id { get; set; }
    }

    public class InstagramWebhookMessage
    {
        public string Mid { get; set; }
        public string Text { get; set; }
        public List<InstagramWebhookAttachment> Attachments { get; set; }
    }

    public class InstagramWebhookAttachment
    {
        public string Type { get; set; }
        public InstagramWebhookPayload Payload { get; set; }
    }

    public class InstagramWebhookPayload
    {
        public string Url { get; set; }
    }
}
