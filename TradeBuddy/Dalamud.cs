using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace TradeBuddy
{
	public class Dalamud
	{
		public static void Initialize(DalamudPluginInterface pluginInterface) =>
			pluginInterface.Create<Dalamud>();

		[PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static CommandManager Commands { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static DataManager DataManager { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static ChatGui ChatGui { get; private set; } = null!;

	}
}
