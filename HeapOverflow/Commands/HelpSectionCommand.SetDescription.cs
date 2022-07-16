using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HeapOverflow.AutocompleteProviders;
using X10D.Text;

namespace HeapOverflow.Commands;

internal sealed partial class HelpSectionCommand
{
    [SlashCommand("setdescription", "Sets the description of a category in the Development & Help section.", false)]
    public async Task SetDescriptionAsync(
        InteractionContext context,
        [Option("category", "The category to rename."), Autocomplete(typeof(CategoryAutocompleteProvider))] string id,
        [Option("description", "The new description of the category.")] string description
    )
    {
        DiscordGuild guild = context.Guild;
        var embed = new DiscordEmbedBuilder();

        if (!Guid.TryParse(id, out Guid guid) || _categoryService.GetCategory(guild, guid) is not { } category)
        {
            embed.WithTitle("Category Modification Failed");
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"No category with the ID `{id}` could be found.");

            await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
            return;
        }

        string? oldDescription = category.Description;

        await context.DeferAsync(true).ConfigureAwait(false);
        await _categoryService.ModifyCategoryAsync(category, c => c.Description = description, context.Member)
            .ConfigureAwait(false);

        embed.WithTitle("Category Description Modified");
        embed.WithColor(DiscordColor.Orange);
        embed.AddField("Old Description", oldDescription?.WithWhiteSpaceAlternative("<none>"));
        embed.AddField("New Description", category.Description?.WithWhiteSpaceAlternative("<none>"));

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }
}
