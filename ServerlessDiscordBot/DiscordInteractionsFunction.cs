using Discord;
using Discord.Interactions;
using Discord.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ServerlessDiscordBot.Commands;
using ServerlessDiscordBot.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServerlessDiscordBot
{
    public static class DiscordInteractionsFunction
    {
        private static readonly string PublicKey = Environment.GetEnvironmentVariable("DiscordPublicKey");
        private static readonly string BotToken = Environment.GetEnvironmentVariable("DiscordBotToken");

        private static readonly DiscordRestClient _client = new();
        private static readonly InteractionService _interactionService = new(_client);

        [FunctionName("HandleDiscordInteraction")]
        public static async Task<IActionResult> Run(
             [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
             ILogger log)
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
                    log.LogInformation($"Responding to slash command.");

                    // Set up InteractionService and register SlashCommandModule dynamically
                    if (!_interactionService.Modules.Any(module => module.Name == "SlashCommandModule"))
                    {
                        await _interactionService.AddModuleAsync<SlashCommandModule>(null);
                    }

                    // Create an Interaction Context for the InteractionService
                    var interactionContext = new RestInteractionContext(_client, interaction);

                    // Execute the command from SlashCommandModule
                    var result = await _interactionService.ExecuteCommandAsync(interactionContext, null);

                    if (!result.IsSuccess)
                    {
                        log.LogError($"Error executing command: {result.ErrorReason}");
                        if (interaction.HasResponded)
                        {
                            // Follow up with an error message if command execution failed and interaction was already responded to
                            await interaction.FollowupAsync($"Error: {result.ErrorReason}");
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
    }
}
