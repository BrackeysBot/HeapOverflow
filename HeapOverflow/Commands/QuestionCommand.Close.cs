using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HeapOverflow.Data;

namespace HeapOverflow.Commands;

internal sealed partial class QuestionCommand
{
    [SlashCommand("close", "Closes a question.", false)]
    public async Task CloseAsync(InteractionContext context,
        [Option("reason", "The reason for the closure.")] CloseReason reason)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        var builder = new DiscordWebhookBuilder();
        DiscordChannel channel = context.Channel;

        if (!channel.IsThread)
        {
            builder.WithContent("You can only run this command from a thread.");
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        if (!_questionService.IsQuestionChannel(channel))
        {
            if (!await _questionService.IsArchivedQuestionChannelAsync(channel).ConfigureAwait(false))
            {
                builder.WithContent("You can only run this command from a question thread.");
                await context.EditResponseAsync(builder).ConfigureAwait(false);
                return;
            }
        }

        Question question = (await _questionService.GetQuestionFromThreadAsync(channel).ConfigureAwait(false))!;
        await _questionService.CloseAsync(question, reason, context.Member).ConfigureAwait(false);

        builder.WithContent($"Question {question.Id} has been closed.");
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
