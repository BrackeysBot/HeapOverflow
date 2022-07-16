using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.API.Plugins;
using BrackeysBot.Core.API;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using HeapOverflow.Commands;
using HeapOverflow.Data;
using HeapOverflow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HeapOverflow;

/// <summary>
///     Represents a class which implements the HeapOverflow plugin.
/// </summary>
[Plugin("HeapOverflow")]
[PluginDependencies("BrackeysBot.Core")]
[PluginDescription("A BrackeysBot plugin for organising the help section.")]
[PluginIntents(DiscordIntents.AllUnprivileged | DiscordIntents.GuildMessages)] // message content is privileged as of Aug 2022
public sealed class HeapOverflowPlugin : MonoPlugin
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(PluginManager.GetPlugin<ICorePlugin>()!);

        services.AddHostedSingleton<CachedMessageService>();
        services.AddHostedSingleton<QuestionService>();
        services.AddHostedSingleton<QuestionCategoryService>();
        services.AddHostedSingleton<QuestionSubmissionService>();

        services.AddDbContext<HeapOverflowContext>();
    }

    /// <inheritdoc />
    protected override Task OnLoad()
    {
        DiscordClient.GuildAvailable += OnGuildAvailable;
        return base.OnLoad();
    }

    private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        Logger.Info("Registering commands");
        SlashCommandsExtension slashCommands = sender.GetSlashCommands();
        slashCommands.RegisterCommands<AskHereCommand>(e.Guild.Id);
        slashCommands.RegisterCommands<HelpSectionCommand>(e.Guild.Id);
        slashCommands.RegisterCommands<QuestionCommand>(e.Guild.Id);
        return slashCommands.RefreshCommands();
    }
}
