using Discord.Interactions;
using Discord.Rest;
using Microsoft.Extensions.Logging;
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
        }
    }
}
