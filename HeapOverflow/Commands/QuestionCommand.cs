using DSharpPlus.SlashCommands;
using HeapOverflow.Services;

namespace HeapOverflow.Commands;

/// <summary>
///     Represents a class which implements the <c>question</c> command.
/// </summary>
[SlashCommandGroup("question", "Manages question threads.", false)]
internal sealed partial class QuestionCommand : ApplicationCommandModule
{
    private readonly QuestionService _questionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="QuestionCommand" /> class.
    /// </summary>
    /// <param name="questionService">The question service.</param>
    public QuestionCommand(QuestionService questionService)
    {
        _questionService = questionService;
    }
}
