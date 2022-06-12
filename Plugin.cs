using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using TradeBuddy.Window;

namespace TradeBuddy
{
	public sealed class Plugin : IDalamudPlugin
	{
		public string Name => "Trade Buddy";

		private const string commandName = "/tb";

		public DalamudPluginInterface PluginInterface { get; init; }
		public CommandManager CommandManager { get; init; }
		public Configuration Configuration { get; init; }
		public History History { get; init; }
		public PluginUI PluginUi { get; init; }

		public static Plugin Instance { get; private set; }

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager
		)
		{
			Instance = this;
			this.PluginInterface = pluginInterface;
			this.CommandManager = commandManager;
			History = new History();

			DalamudDll.DalamudInitialize(pluginInterface);
			this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			this.Configuration.Initialize(this.PluginInterface);

			//var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
			//var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
			this.PluginUi = new PluginUI(this.Configuration);

			this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
			{
				HelpMessage = "/tb 打开历史记录" +"\n /tb config|cfg 打开设置窗口"
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
			string arg = args.Trim().Replace("\"", string.Empty);
			if (string.IsNullOrEmpty(arg))
			{
				this.PluginUi.tradeOnceVisible = true;
				this.PluginUi.historyVisible = true;
			}
			if (arg == "cfg" || arg == "config")
				this.PluginUi.SettingsVisible = true;
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
