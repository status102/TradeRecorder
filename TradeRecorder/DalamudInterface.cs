using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace TradeRecorder
{
    public class DalamudInterface
    {
        public static void Initialize(DalamudPluginInterface pluginInterface) => pluginInterface.Create<DalamudInterface>();

        [PluginService][RequiredVersion("1.0")] public static ObjectTable ObjectTable { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static CommandManager Commands { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static DataManager DataManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ChatGui ChatGui { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ClientState ClientState { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static GameGui GameGui { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static GameNetwork GameNetwork { get; private set; } = null!;

    }
}
