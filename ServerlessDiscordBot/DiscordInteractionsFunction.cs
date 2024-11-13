using Discord;
using Discord.Interactions;
using Discord.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServerlessDiscordBot.Commands;
using ServerlessDiscordBot.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServerlessDiscordBot
{
    [SupportedOSPlatform("windows")]
    public static class DiscordInteractionsFunction
    {
        private static readonly string PublicKey = Environment.GetEnvironmentVariable("DiscordPublicKey");
        private static readonly string BotToken = Environment.GetEnvironmentVariable("DiscordBotToken");

        private static readonly HttpClient httpClient = new();
        private static readonly DiscordRestClient _client = new();
        private static readonly InteractionService _interactionService = new(_client, new()
        {
            DefaultRunMode = RunMode.Sync,
            LogLevel = LogSeverity.Debug
        });

        [FunctionName("HandleDiscordInteraction")]
        public static async Task<IActionResult> Run(
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log, ExecutionContext context)
        {
            DiscordLogService logService = new(_client, _interactionService, log);
            try
            {
                log.LogInformation("Request to handle Discord interaction.");

                // Log in with the bot
                await _client.LoginAsync(TokenType.Bot, BotToken);

                // Read the request body as a byte array for signature validation
                byte[] bodyBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await req.Body.CopyToAsync(memoryStream);
                    bodyBytes = memoryStream.ToArray();
                }

                // Parse the interaction using DiscordRestClient
                var interaction = await _client.ParseHttpInteractionAsync(PublicKey, req.Headers["X-Signature-Ed25519"], req.Headers["X-Signature-Timestamp"], bodyBytes);

                // Check interaction type
                if (interaction.Type == InteractionType.Ping)
                {
                    log.LogInformation("Responding to ping.");
                    return new JsonResult(new { type = InteractionResponseType.Pong });
                }
                else if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    log.LogInformation("Responding to slash command.");

                    var serviceProvider = new ServiceCollection()
                        .AddSingleton(log)
                        .AddSingleton(context)
                        .AddSingleton<SlashCommandModule>()
                        .BuildServiceProvider();

                    // Set up InteractionService and register SlashCommandModule dynamically
                    if (!_interactionService.Modules.Any(module => module.Name == "SlashCommandModule"))
                    {
                        log.LogInformation("Registering SlashCommandModule");
                        await _interactionService.AddModuleAsync<SlashCommandModule>(serviceProvider);
                    }

                    // Create an Interaction Context for the InteractionService
                    var interactionContext = new RestInteractionContext(_client, interaction);
                    interactionContext.InteractionResponseCallback = (payload) => SendInteractionResponseAsync(interactionContext, payload);

                    // Execute the command from SlashCommandModule
                    log.LogInformation("Executing command from SlashCommandModule");
                    var result = await _interactionService.ExecuteCommandAsync(interactionContext, serviceProvider);

                    if (!result.IsSuccess)
                    {
                        log.LogError($"Error executing command: {result.ErrorReason}");
                        if (interaction.HasResponded)
                        {
                            var response = await interaction.GetOriginalResponseAsync();
                            if (response.Flags.Value.HasFlag(MessageFlags.Loading))
                            {
                                if (!response.Flags.Value.HasFlag(MessageFlags.Ephemeral))
                                {
                                    await interaction.DeleteOriginalResponseAsync(); // Original Defer was not ephemeral and this can't be changed, so we first delete it.
                                }
                                // Follow up with ephemeral error message if command execution failed and interaction was deferred
                                await interaction.FollowupAsync($"Error: {result.ErrorReason}", ephemeral: true);
                            }
                            return new AcceptedResult();
                        }
                        return new BadRequestObjectResult(new { content = $"Error: {result.ErrorReason}" });
                    }

                    if (interaction.HasResponded)
                    {
                        log.LogInformation("Already successfully responded to the command, returning 202.");
                        return new AcceptedResult();
                    }

                    log.LogInformation("Successfully handled the command without any response, responding with ephemeral message.");
                    return new ContentResult
                    {
                        Content = interaction.Respond("Success!", ephemeral: true),
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
            }
            catch (BadSignatureException)
            {
                log.LogWarning("Bad signature of the request");
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                // Log and handle any parsing or validation errors
                log.LogError(ex, $"Error processing interaction: {ex.Message}");
                return new InternalServerErrorResult();
            }

            return new BadRequestResult();
        }

        private static async Task SendInteractionResponseAsync(IRestInteractionContext context, string payload)
        {
            var interaction = context.Interaction;
            var url = $"https://discord.com/api/v10/interactions/{interaction.Id}/{interaction.Token}/callback";

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            await httpClient.PostAsync(url, content);
        }
    }
}
