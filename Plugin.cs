using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;

namespace TradeBuddy
{
	public sealed class Plugin : IDalamudPlugin
	{
		public string Name => "Trade Buddy";

		private const string commandName = "/tb";

		[PluginService][RequiredVersion("1.0")] internal GameGui GameGui { get; init; }
		[PluginService][RequiredVersion("1.0")] internal ChatGui ChatGui { get; init; }
		[PluginService][RequiredVersion("1.0")] internal DataManager DataManager { get; init; }
		[PluginService][RequiredVersion("1.0")] internal GameNetwork GameNetwork { get; init; }

		public DalamudPluginInterface PluginInterface { get; init; }
		public CommandManager CommandManager { get; init; }
		public Configuration Configuration { get; init; }
		public PluginUI PluginUi { get; init; }

		public static Plugin plugin { get; set; }

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager)
		{
			plugin = this;
			this.PluginInterface = pluginInterface;
			this.CommandManager = commandManager;

			DalamudDll.DalamudInitialize(pluginInterface);
			this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			this.Configuration.Initialize(this.PluginInterface);

			// you might normally want to embed resources and load them from the manifest stream
			var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
			var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
			this.PluginUi = new PluginUI(this.Configuration, goatImage);

			this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
			{
				HelpMessage = "A useful message to display in /xlhelp"
			});

			this.PluginInterface.UiBuilder.Draw += DrawUI;
			this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

			DalamudDll.ChatGui.Print("测试插件载入成功");
		}

		public void Dispose()
		{
			this.PluginUi.Dispose();
			this.CommandManager.RemoveHandler(commandName);
		}

		private void OnCommand(string command, string args)
		{
			// in response to the slash command, just display our main ui
			this.PluginUi.Visible = true;
			this.PluginUi.tradeOnceVisible = true;
		}

		private void DrawUI()
		{
			this.PluginUi.Draw();
		}

		private void DrawConfigUI()
		{
			this.PluginUi.SettingsVisible = true;
		}
	}
}
