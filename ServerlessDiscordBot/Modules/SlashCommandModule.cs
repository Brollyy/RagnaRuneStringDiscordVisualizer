using Discord.Interactions;
using Discord.Rest;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using RagnaRuneStringVisualizer;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace ServerlessDiscordBot.Commands
{
    [SupportedOSPlatform("windows")]
    public class SlashCommandModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private static readonly string DiscordAdminUserId = Environment.GetEnvironmentVariable("DiscordAdminUserId"); // Add your Discord ID in your settings
        public ILogger Log { get; set; }
        public ExecutionContext AzureContext { get; set; }

        [SlashCommand("runestring-image", "Renders an image that shows the contents of the runestring")]
        public async Task RunestringImageAsync(
            [Summary(description: "Runestring - cannot be longer than 5 beats and contain more than 50 runes")]
            string runestring
        )
        {
            Log.LogInformation($"Generating RuneString image for {runestring}");
            await DeferAsync(ephemeral: true);

            Log.LogInformation("Deferred response, processing image...");
            try
            {
                using var imageGenerator = new ImageGenerator(runestring.Trim());
                try
                {
                    using var imageStream = new MemoryStream();
                    imageGenerator.RenderToStream(imageStream, ImageFormat.Png);
                    imageStream.Position = 0; // Reset to start after writing

                    Log.LogInformation($"Responding with RuneString image for {runestring}");
                    await FollowupWithFileAsync(imageStream, $"Runestring-{runestring[..Math.Min(runestring.Length, 252)]}.png", text: $"`{runestring}`");
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, $"Unexpected error rendering RuneString image for {runestring}");
                    await FollowupAsync($"Uh oh... Looks like there was an unexpected issue creating the image. Please DM <@{DiscordAdminUserId}> and include this operation ID in your message - `{AzureContext.InvocationId}`.", allowedMentions: Discord.AllowedMentions.All, ephemeral: true);
                }
            }
            catch (ArgumentException ex)
            {
                Log.LogWarning($"Invalid input for RuneString image - {runestring}");
                await FollowupAsync(ex.Message, ephemeral: true);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, $"Unexpected error rendering RuneString image for {runestring}");
                await FollowupAsync($"Uh oh... Looks like there was an unexpected issue creating the image. Please DM <@{DiscordAdminUserId}> and include this operation ID in your message - `{AzureContext.InvocationId}`.", allowedMentions: Discord.AllowedMentions.All, ephemeral: true);
            }
        }
    }
}
