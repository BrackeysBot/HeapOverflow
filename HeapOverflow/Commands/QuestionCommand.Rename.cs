using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace HeapOverflow.Commands;

internal sealed partial class QuestionCommand
{
    [SlashCommand("rename", "Rename a question's title.", false)]
    public async Task RenameAsync(InteractionContext context, [Option("title", "The new title of the question.")] string title)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        if (!context.Channel.IsThread)
        {
            var builder = new DiscordWebhookBuilder();
            builder.WithContent("You can only run this command from a thread.");
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        if (!_questionService.IsQuestionChannel(context.Channel))
        {
            if (!await _questionService.IsArchivedQuestionChannelAsync(context.Channel))
            {
                var builder = new DiscordWebhookBuilder();
                builder.WithContent("You can only run this command from a question thread.");
                await context.EditResponseAsync(builder).ConfigureAwait(false);
                return;
            }
        }
    }
}
