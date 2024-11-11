using Discord.Interactions;
using Discord.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ServerlessDiscordBot
{
    public static class RegisterSlashCommandsFunction
    {
        private static readonly string BotToken = Environment.GetEnvironmentVariable("DiscordBotToken");

        private static readonly DiscordRestClient _client = new();
        private static readonly InteractionService _interactionService = new(_client);

        [FunctionName("RegisterSlashCommands")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("Request to register slash commands started.");

                // Log in with the bot
                await _client.LoginAsync(Discord.TokenType.Bot, BotToken);

                // Add all modules (this automatically registers commands defined in the modules)
                await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), null);

                log.LogInformation("Registering all slash commands globally.");
                await _interactionService.RegisterCommandsGloballyAsync();

                log.LogInformation("Successfully registered all commands.");
                return new OkObjectResult("Commands registered successfully.");
            }
            catch (Exception ex)
            {
                log.LogError($"Error registering commands: {ex.Message}");
                return new BadRequestObjectResult($"Failed to register commands: {ex.Message}");
            }
        }
    }
}
