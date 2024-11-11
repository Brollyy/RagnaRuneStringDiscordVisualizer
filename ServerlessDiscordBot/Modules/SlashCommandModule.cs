using Discord.Interactions;
using Discord.Rest;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace ServerlessDiscordBot.Commands
{
    public class SlashCommandModule : RestInteractionModuleBase<RestInteractionContext>
    {
        public static ILogger Log { get; set; }

        [SlashCommand("runestring-image", "Renders an image that shows the contents of the runestring")]
        public async Task RunestringImageAsync(
            [Summary(description: "Runestring - cannot be longer than 5 beats and contain more than 50 runes")]
            string runestring
        )
        {
            Log.LogInformation($"Generating RuneString image for {runestring}");
            await DeferAsync();

            Log.LogInformation("Deferred response, processing...");
            await Task.Delay(5000); // Processing
            using var imageStream = File.OpenRead("Image.bmp");

            Log.LogInformation($"Responding with RuneString image for {runestring}");
            await FollowupWithFileAsync(imageStream, "Image.bmp");
        }
    }
}
