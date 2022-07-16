using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HeapOverflow.Services;

namespace HeapOverflow.Commands;

/// <summary>
///     Represents a class which implements the <c>askhere</c> command.
/// </summary>
internal sealed class AskHereCommand : ApplicationCommandModule
{
    private readonly QuestionSubmissionService _submissionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AskHereCommand" /> class.
    /// </summary>
    /// <param name="submissionService">The question submission service.</param>
    public AskHereCommand(QuestionSubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [SlashCommand("askhere", "Posts the \"Ask Here\" embed to the channel.", false)]
    public async Task AskHereAsync(InteractionContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        await _submissionService.PostQuestionSubmissionEmbedAsync(context.Channel).ConfigureAwait(false);

        var builder = new DiscordWebhookBuilder();
        builder.WithContent("Ask Here embed posted.");
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
