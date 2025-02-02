using System.Diagnostics;

namespace WebHookSystem.OpenTelemetry
{
    internal static class DiagnosticConfig
    {
        internal static readonly ActivitySource Source = new("webhooks-api");
    }
}
