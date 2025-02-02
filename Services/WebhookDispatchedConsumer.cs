using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebHookSystem.Data;

namespace WebHookSystem.Services
{
    internal sealed class WebhookDispatchedConsumer(WebhookDbContext dbContext) : IConsumer<WebhookDispatched>
    {
        public async Task Consume(ConsumeContext<WebhookDispatched> context)
        {
            var message = context.Message;

            var subscriptions = await dbContext.WebhookSubscriptions
                .AsNoTracking()
                .Where(s => s.EventType == message.EventType)
                .ToListAsync();

            foreach ( var subscription in subscriptions)
            {
                await context.Publish(new WebhookTriggered(
                    subscription.Id,
                    subscription.EventType,
                    subscription.WebhookUrl,
                    message.Data));
            }
        }
    }
}


