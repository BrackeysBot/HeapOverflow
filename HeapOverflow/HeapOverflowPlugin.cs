using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.API.Plugins;
using BrackeysBot.Core.API;
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
public sealed class HeapOverflowPlugin : MonoPlugin
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(PluginManager.GetPlugin<ICorePlugin>()!);

        services.AddHostedSingleton<QuestionService>();

        services.AddDbContext<HeapOverflowContext>();
    }

    /// <inheritdoc />
    protected override Task OnLoad()
    {
        Logger.Info("Hello World!");
        return base.OnLoad();
    }

    /// <inheritdoc />
    protected override Task OnUnload()
    {
        Logger.Info("Goodbye world!");
        return base.OnUnload();
    }
}
