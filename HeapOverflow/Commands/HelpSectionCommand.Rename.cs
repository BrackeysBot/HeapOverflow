using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HeapOverflow.AutocompleteProviders;

namespace HeapOverflow.Commands;

internal sealed partial class HelpSectionCommand
{
    [SlashCommand("renamecategory", "Renames a category in the Development & Help section.", false)]
    public async Task RenameAsync(
        InteractionContext context,
        [Option("category", "The category to rename."), Autocomplete(typeof(CategoryAutocompleteProvider))] string id,
        [Option("name", "The new name of the category.")] string name
    )
    {
        DiscordGuild guild = context.Guild;
        var embed = new DiscordEmbedBuilder();

        if (!Guid.TryParse(id, out Guid guid) || _categoryService.GetCategory(guild, guid) is not { } category)
        {
            embed.WithTitle("Category Rename Failed");
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"No category with the ID `{id}` could be found.");

            await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
            return;
        }

        string oldName = category.Name;

        await context.DeferAsync(true).ConfigureAwait(false);
        await _categoryService.ModifyCategoryAsync(category, c => c.Name = name, context.Member).ConfigureAwait(false);

        embed.WithTitle("Category Renamed");
        embed.WithColor(DiscordColor.Orange);
        embed.AddField("Old Name", oldName, true);
        embed.AddField("New Name", category.Name, true);

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }
}
