using MassTransit;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using WebHookSystem.Data;
using WebHookSystem.Model;
using WebHookSystem.OpenTelemetry;

namespace WebHookSystem.Services
{

    

    internal sealed class WebHookDispatcher
    {

        private readonly IPublishEndpoint _publishEndpoint;
        public WebHookDispatcher(IPublishEndpoint publishEndpoint)
        {         
            _publishEndpoint = publishEndpoint;
        }

        public async Task DispatchAsync<T>(string eventType,T data)
            where T : notnull
        {
            using Activity? activity = DiagnosticConfig.Source.StartActivity($"{eventType} dispatch webhook");
            activity?.AddTag("event.type",eventType);
            await _publishEndpoint.Publish(new WebhookDispatched(eventType, data)); 
        }

        }
    }

