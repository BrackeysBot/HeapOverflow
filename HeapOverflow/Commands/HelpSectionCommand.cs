using DSharpPlus.SlashCommands;
using HeapOverflow.Services;

namespace HeapOverflow.Commands;

[SlashCommandGroup("helpsection", "Manages the Development & Help section.", false)]
internal sealed partial class HelpSectionCommand : ApplicationCommandModule
{
    private readonly QuestionCategoryService _categoryService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HelpSectionCommand" /> class.
    /// </summary>
    /// <param name="categoryService">The question category service.</param>
    public HelpSectionCommand(QuestionCategoryService categoryService)
    {
        _categoryService = categoryService;
    }
}
