using System.Threading.Tasks;
using BrackeysBot.API.Plugins;

namespace HeapOverflow;

/// <summary>
///     Represents a class which implements the HeapOverflow plugin.
/// </summary>
[Plugin("HeapOverflow")]
[PluginDescription("A BrackeysBot plugin for organising the help section.")]
public sealed class HeapOverflowPlugin : MonoPlugin
{
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
