using Discord.Interactions;
using Discord.Rest;
using System.Threading.Tasks;

namespace ServerlessDiscordBot.Commands
{
    public class SlashCommandModule : RestInteractionModuleBase<RestInteractionContext>
    {
        [SlashCommand("runestring-image", "Renders an image that shows the contents of the runestring", false, RunMode.Sync)]
        public async Task RunestringImageAsync(
            [Summary(description: "Runestring - cannot be longer than 5 beats and contain more than 50 runes")]
            string runestring
        )
        {
            await DeferAsync();
        }
    }
}
