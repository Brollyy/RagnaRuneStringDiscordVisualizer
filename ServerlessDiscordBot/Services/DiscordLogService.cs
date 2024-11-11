using Discord;
using Discord.Interactions;
using Discord.Rest;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ServerlessDiscordBot.Services
{
    internal class DiscordLogService
    {
        private readonly ILogger log;

        public DiscordLogService(DiscordRestClient client, InteractionService interactionService, ILogger log)
        {
            this.log = log;
            client.Log += LogAsync;
            interactionService.Log += LogAsync;
        }

        private Task LogAsync(LogMessage message)
        {
            log.Log(message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.None
            }, message.Exception, $"{message}");

            return Task.CompletedTask;
        }
    }
}
