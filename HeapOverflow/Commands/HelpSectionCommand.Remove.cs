using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HeapOverflow.AutocompleteProviders;

namespace HeapOverflow.Commands;

internal sealed partial class HelpSectionCommand
{
    [SlashCommand("removecategory", "Removes a category from the Development & Help section.", false)]
    public async Task RemoveAsync(
        InteractionContext context,
        [Option("category", "The category to remove."), Autocomplete(typeof(CategoryAutocompleteProvider))] string id
    )
    {
        DiscordGuild guild = context.Guild;
        var embed = new DiscordEmbedBuilder();

        if (!Guid.TryParse(id, out Guid guid) || _categoryService.GetCategory(guild, guid) is not { } category)
        {
            embed.WithTitle("Category Creation Failed");
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"No category with the ID `{id}` could be found.");

            await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync(true).ConfigureAwait(false);
        await _categoryService.DeleteCategoryAsync(category, context.Member).ConfigureAwait(false);

        embed.WithTitle("Category Deleted");
        embed.WithColor(DiscordColor.Green);
        embed.AddField("Name", category.Name, true);

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }
}
