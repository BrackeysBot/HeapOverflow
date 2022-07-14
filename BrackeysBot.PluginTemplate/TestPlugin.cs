using System.Threading.Tasks;
using BrackeysBot.API.Plugins;

namespace BrackeysBot.PluginTemplate;

/// <summary>
///     Represents a class which implements an example plugin.
/// </summary>
[Plugin("TestPlugin")]
[PluginDescription("An empty template plugin to demonstrate the usage of the BrackeysBot API.")]
public sealed class TestPlugin : MonoPlugin
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
