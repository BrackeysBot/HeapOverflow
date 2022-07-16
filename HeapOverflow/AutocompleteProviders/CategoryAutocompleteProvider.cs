using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HeapOverflow.Data;
using HeapOverflow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HeapOverflow.AutocompleteProviders;

/// <summary>
///     Represents an autocomplete provider which autocompletes a <see cref="QuestionCategory" />.
/// </summary>
internal sealed class CategoryAutocompleteProvider : IAutocompleteProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        var categoryService = context.Services.GetRequiredService<QuestionCategoryService>();

        return Task.FromResult(categoryService.GetCategories(context.Guild)
            .Select(category => new DiscordAutoCompleteChoice(category.Name, category.Id.ToString("N"))));
    }
}
