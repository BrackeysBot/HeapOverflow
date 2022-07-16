using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HeapOverflow.Data;

namespace HeapOverflow.Commands;

internal sealed partial class HelpSectionCommand
{
    [SlashCommand("addcategory", "Add a new category to the Development & Help section.", false)]
    public async Task AddAsync(
        InteractionContext context,
        [Option("name", "The name of the category")] string name,
        [Option("description", "Optional. A description override. If not specified, the channel topic is used.")]
        string? description = null
    )
    {
        var embed = new DiscordEmbedBuilder();

        if (string.IsNullOrWhiteSpace(name))
        {
            embed.WithTitle("Category Creation Failed");
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription("You must specify a name.");
            await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
            return;
        }

        DiscordGuild guild = context.Guild;

        if (_categoryService.GetCategory(guild, name) is not null)
        {
            embed.WithTitle("Category Creation Failed");
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"A category with the name `{name}` already exists.");

            await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync(true).ConfigureAwait(false);

        QuestionCategory category =
            await _categoryService.CreateCategoryAsync(guild, context.Member, name, description).ConfigureAwait(false);

        embed.WithTitle("New Category Created");
        embed.WithColor(DiscordColor.Green);
        embed.AddField("Name", category.Name, true);
        embed.AddField("Description", category.Description);

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }
}
