using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebHookSystem.Data;
using WebHookSystem.Model;
using WebHookSystem.Services;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using WebHookSystem.OpenTelemetry;
using MassTransit;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHttpClient<WebHookDispatcher>();
builder.Services.AddScoped<WebHookDispatcher>();
//builder.Services.AddHostedService<WebhookProcessor>();


builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
     tracing
      .AddSource(DiagnosticConfig.Source.Name)
      .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
      .AddNpgsql()


    );


/*builder.Services.AddSingleton(_ =>
{
    return Channel.CreateBounded<WebhookDispatch>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });
});*/

builder.Services.AddMassTransit(busConfig =>
{
    busConfig.SetKebabCaseEndpointNameFormatter();
    busConfig.AddConsumer<WebhookDispatchedConsumer>();
    busConfig.AddConsumer<WebhookTriggeredConsumer>();

    busConfig.UsingRabbitMq((context, config) =>
    {
        config.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        config.ConfigureEndpoints(context);
    });
});
builder.Services.AddDbContext<WebhookDbContext>(config =>
  config.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));


var app = builder.Build();

app.MapPost("/webhooks/subscriptions", async (
    [FromBody] CreateWebhookRequest request,  
     WebhookDbContext dbContext) =>
{
    var subscription = new WebhookSubscription(
        Guid.NewGuid(),
        request.EventType,
        request.WebhookUrl,
        DateTime.UtcNow);

    dbContext.WebhookSubscriptions.Add(subscription);

    await dbContext.SaveChangesAsync();

    return Results.Ok(subscription);
})
    .WithTags("Order");

app.MapPost("/orders/create", async (
    [FromBody] CreateOrderRequest request,  // Bind body to CreateOrderRequest
    WebhookDbContext dbContext,
    [FromServices] WebHookDispatcher webHookDispatcher) =>
{
    var order = new Order(Guid.NewGuid(), request.CustomerName, request.Amount, DateTime.UtcNow);

    dbContext.Orders.Add(order);

    await webHookDispatcher.DispatchAsync("order.created", order);

    return Results.Ok(order);
})
    .WithTags("Order");

app.MapGet("/orders", async (
     WebhookDbContext dbContext) =>  // Inject only the service
{
    return Results.Ok(await dbContext.Orders.ToListAsync());
})
    .WithTags("Order");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();


    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
        await dbContext.Database.MigrateAsync();  // Apply pending migrations
    }

}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    await dbContext.Database.MigrateAsync();  // Apply pending migrations
}



app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
