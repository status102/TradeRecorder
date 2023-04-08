using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;

namespace TradeRecorder
{
	public sealed class TradeRecorder : IDalamudPlugin
	{
		public static string PluginName { get; } = "TradeRec.";
		public string Name => "Trade Recorder";
		private const string commandName = "/tr";
		public static TradeRecorder? Instance { get; private set; }
		public PluginUI PluginUi { get; init; }
		public Configuration Config { get; init; }
		public DalamudPluginInterface PluginInterface { get; private set; }
		public uint homeWorldId = 0;

		public TradeRecorder([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface) {
			Instance = this;
			PluginInterface = pluginInterface;

			DalamudInterface.Initialize(pluginInterface);
			Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			Config.Initialize(PluginInterface);

			DalamudInterface.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
				HelpMessage = "/tr 打开历史记录\n /tr config|cfg 打开设置窗口"
			});

			PluginInterface.UiBuilder.Draw += DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			DalamudInterface.ClientState.Login += OnLogin;
			DalamudInterface.ClientState.Logout += OnLogout;

			PluginUi = new PluginUI(this, Config);
			homeWorldId = DalamudInterface.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;
		}

		public void Dispose() {
			DalamudInterface.ClientState.Login -= OnLogin;
			DalamudInterface.ClientState.Logout -= OnLogout;
			PluginInterface.UiBuilder.Draw -= DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			DalamudInterface.CommandManager.RemoveHandler(commandName);
			PluginUi.Dispose();
		}

		private unsafe void OnCommand(string command, string args) {
			string arg = args.Trim().Replace("\"", string.Empty);
			if (string.IsNullOrEmpty(arg)) {
				PluginUi.History.ShowHistory();
			} else if (arg == "cfg" || arg == "config") { PluginUi.Setting.Show(); }
#if DEBUG
			else if (arg == "test") {
				Chat.PrintLog("服务器id:" + homeWorldId);
			}
#endif
		}

		private void DrawUI() {
			PluginUi.Draw();
		}

		private void DrawConfigUI() {
			this.PluginUi.Setting.Show();
		}

		private void OnLogin(object? sender, EventArgs e) {
			homeWorldId = DalamudInterface.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;
		}

		private void OnLogout(object? sender, EventArgs e) {
			homeWorldId = 0;
		}
	}
}
