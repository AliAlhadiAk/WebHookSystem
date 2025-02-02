using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using WebHookSystem.Data;
using WebHookSystem.Model;

namespace WebHookSystem.Services
{
    internal sealed class WebhookTriggeredConsumer(
        IHttpClientFactory httpClientFactory,
        WebhookDbContext dbContext) : IConsumer<WebhookTriggered>
    {
        public async Task Consume(ConsumeContext<WebhookTriggered> context)
        {
            using var httpClient = httpClientFactory.CreateClient();

            WebhookPayload payload = new WebhookPayload
            {
                Id = Guid.NewGuid(),
                EventType = context.Message.EventType,
                SubscriptionId = context.Message.SubscriptionId,
                Timestamp = DateTime.UtcNow,
                Data = context.Message.Data
            };

            Console.WriteLine($"EventType: {context.Message.EventType}");
            Console.WriteLine($"SubscriptionId: {context.Message.SubscriptionId}");
            Console.WriteLine($"Data: {context.Message.Data}");

            var jsonPayload = JsonSerializer.Serialize(payload);

            try
            {
                var response = await httpClient.PostAsJsonAsync(context.Message.WebhookUrl, payload);
                response.EnsureSuccessStatusCode();

                var attempt = new WebhookDiliveryAttempt
                {
                    Id = Guid.NewGuid(),
                    WebhookSubscriptionId = context.Message.SubscriptionId,
                    Payload = jsonPayload,
                    ResponseStatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode,
                    TimeStamp = DateTime.UtcNow,
                };

                // Log delivery attempt to the database
                dbContext.WebhookDiliveryAttempts.Add(attempt);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var attempt = new WebhookDiliveryAttempt
                {
                    Id = Guid.NewGuid(),
                    WebhookSubscriptionId = context.Message.SubscriptionId,
                    Payload = jsonPayload,
                    ResponseStatusCode = null,
                    Success = false,
                    TimeStamp = DateTime.UtcNow,
                };

                // Log failed delivery attempt to the database
                await dbContext.WebhookDiliveryAttempts.AddAsync(attempt);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
