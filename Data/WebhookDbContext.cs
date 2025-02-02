using Microsoft.EntityFrameworkCore;
using WebHookSystem.Model;

namespace WebHookSystem.Data
{
    internal sealed class WebhookDbContext(DbContextOptions<WebhookDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }

        public DbSet<WebhookDiliveryAttempt> WebhookDiliveryAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(builder =>
            {
                builder.ToTable("orders", "webhooks");
                builder.HasKey(o => o.Id);
            });

            modelBuilder.Entity<WebhookSubscription>(builder =>
            {
                builder.ToTable("subscriptions", "webhooks");
                builder.HasKey(o => o.Id);
            });

            modelBuilder.Entity<WebhookDiliveryAttempt>(builder =>
            {
                builder.ToTable("dilivery_attempts", "webhooks");
                builder.HasKey(o => o.Id);

                builder.HasOne<WebhookSubscription>()
                .WithMany()
                .HasForeignKey(o => o.WebhookSubscriptionId);
            });
        }
    }
}
