using Discord;
using Discord.Interactions;
using Discord.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ServerlessDiscordBot.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServerlessDiscordBot
{
    public static class DiscordInteractionsFunction
    {
        private static readonly string PublicKey = Environment.GetEnvironmentVariable("DiscordPublicKey");

        private static readonly DiscordRestClient _client = new();
        private static readonly InteractionService _interactionService = new(_client);

        [FunctionName("HandleDiscordInteraction")]
        public static async Task<IActionResult> Run(
             [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {
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
                    return new JsonResult(new { type = InteractionResponseType.Pong });
                }
                else if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    // Set up InteractionService and register SlashCommandModule dynamically
                    await _interactionService.AddModuleAsync<SlashCommandModule>(null);

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

                    return new AcceptedResult();
                }
            }
            catch (BadSignatureException)
            {
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                // Log and handle any parsing or validation errors
                log.LogError($"Error processing interaction: {ex.Message}");
                return new UnauthorizedResult();
            }

            return new BadRequestResult();
        }
    }
}
