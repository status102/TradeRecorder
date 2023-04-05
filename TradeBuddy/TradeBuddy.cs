using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TradeBuddy.Universalis;

namespace TradeBuddy
{
    public sealed class TradeBuddy : IDalamudPlugin
	{
		public static string PluginName { get; private set; } = string.Empty;
		public string Name => "Trade Buddy";

		private const string commandName = "/tb";

		public static TradeBuddy? Instance { get; private set; }
		public PluginUI PluginUi { get; init; }
		public Configuration Configuration { get; init; }
		#region 
		public DalamudPluginInterface PluginInterface { get; private set; }
		public CommandManager CommandManager { get; private set; }
		public ClientState ClientState { get; private set; }
		public GameNetwork GameNetwork { get; private set; }
		public GameGui GameGui { get; private set; }
		#endregion

		public TradeBuddy(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager,
			[RequiredVersion("1.0")] ClientState clientState,
			[RequiredVersion("1.0")] GameNetwork gameNetwork,
			[RequiredVersion("1.0")] GameGui gameGui
		) {
			PluginName = Name;
			Instance = this;
			PluginInterface = pluginInterface;
			CommandManager = commandManager;
			ClientState = clientState;
			GameNetwork = gameNetwork;
			GameGui = gameGui;

			DalamudInterface.Initialize(pluginInterface);
			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			Configuration.Initialize(this, PluginInterface);

			PluginUi = new PluginUI(this, Configuration);

			commandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
				HelpMessage = "/tb 打开历史记录\n /tb config|cfg 打开设置窗口"
			});

			PluginInterface.UiBuilder.Draw += DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			clientState.Login += OnLogin;
			clientState.Logout += OnLogout;
		}

		public void Dispose() {
			ClientState.Login -= OnLogin;
			ClientState.Logout -= OnLogout;
			PluginInterface.UiBuilder.Draw -= DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			PluginUi.Dispose();
			CommandManager.RemoveHandler(commandName);
		}

		private unsafe void OnCommand(string command, string args) {
			string arg = args.Trim().Replace("\"", string.Empty);
			if (string.IsNullOrEmpty(arg)) {
				this.PluginUi.History.ShowHistory();
			}
			if (arg == "cfg" || arg == "config")
				this.PluginUi.Setting.Show();
#if DEBUG
			else if (arg == "test") {
				
				Chat.PrintError(DateTimeOffset.FromUnixTimeMilliseconds(1680670639000).ToLocalTime().ToString(Price.format));
			}
#endif
		}

		private void DrawUI() {
			this.PluginUi.Draw();
		}

		private void DrawConfigUI() {
			this.PluginUi.Setting.Show();
		}

		// TODO 注释了onlogin
		private void OnLogin(object? sender, EventArgs e) {
			/*
			System.Action initAction = () => {
				var output = new StringBuilder();
				output.Append($"[{Name}]{Configuration.PresetItemList.Count}个预设");
				var failCount = 0;
				Configuration.PresetItemList.ForEach(i => failCount += (i.minPrice == -1) ? 1 : 0);
				if (failCount != 0)
					output.Append($", {Configuration.PresetItemList.Count}获取失败");
				var expList = new List<string>();
				foreach (var item in Configuration.PresetItemList) {
					if (item.minPrice > 0 && item.minPrice < item.EvaluatePrice())
						expList.Add($"{item.ItemName}: {item.minPrice:#,0}");
				}
				if (expList.Count > 0) {
					output.Append($", {expList.Count}低于预设");
					output.AppendLine();
					output.Append("最低：");
					output.Append(string.Join("; ", expList));
				}
				DalamudInterface.ChatGui.Print(output.ToString());
			};
			Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(_ => {
				Configuration.dcName = Configuration.GetWorldName();
				if (Configuration.PresetItemList.Count == 0)
					return;
				int initCount = 0;

				Configuration.PresetItemList.ForEach((item) => {
					item.UpdateMinPrice(() => {
						if (++initCount >= Configuration.PresetItemList.Count)
							initAction();
					});
				});
			});*/
		}

		private void OnLogout(object? sender, EventArgs e) { }
	}
}
