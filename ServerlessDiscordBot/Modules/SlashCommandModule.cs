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
        public static ILogger Log { get; set; }
        public static ExecutionContext AzureContext { get; set; }

        [SlashCommand("runestring-image", "Renders an image that shows the contents of the runestring")]
        public async Task RunestringImageAsync(
            [Summary(description: "Runestring - cannot be longer than 5 beats and contain more than 50 runes")]
            string runestring
        )
        {
            Log.LogInformation($"Generating RuneString image for {runestring}");
            await DeferAsync();

            Log.LogInformation("Deferred response, processing image...");
            try
            {
                using var imageGenerator = new ImageGenerator(runestring);
                using var imageStream = new MemoryStream();
                imageGenerator.RenderToStream(imageStream, ImageFormat.Png);
                imageStream.Position = 0; // Reset to start after writing

                Log.LogInformation($"Responding with RuneString image for {runestring}");
                await FollowupWithFileAsync(imageStream, $"Runestring-{runestring[..252]}.png");
            }
            catch (ArgumentException ex)
            {
                var operationId = Log.GetType().GetProperty("OperationId")?.GetValue(Log)?.ToString() ?? Guid.NewGuid().ToString();
                Log.LogWarning(ex, $"Couldn't render RuneString image for {runestring}");
                await FollowupAsync($"Uh oh... Looks like there was an issue creating the image. <@{DiscordAdminUserId}>, please check logs with operation ID `{AzureContext.InvocationId}` for more details.", allowedMentions: Discord.AllowedMentions.All);
            }
        }
    }
}
