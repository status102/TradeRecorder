using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;

namespace TradeRecorder
{
	public sealed class TradeRecorder : IDalamudPlugin
	{
		public static string PluginName { get;  } = "TradeRec.";
		public string Name => "Trade Recorder";

		private const string commandName = "/tr";

		public uint homeWorldId = 0;

		public static TradeRecorder? Instance { get; private set; }
		public PluginUI PluginUi { get; init; }
		public Configuration Configuration { get; init; }
		public DalamudPluginInterface PluginInterface { get; private set; }

		public TradeRecorder([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface) {
			Instance = this;
			PluginInterface = pluginInterface;

			DalamudInterface.Initialize(pluginInterface);
			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			Configuration.Initialize(this, PluginInterface);

			DalamudInterface.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
				HelpMessage = "/tr 打开历史记录\n /tr config|cfg 打开设置窗口"
			});

			PluginInterface.UiBuilder.Draw += DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			DalamudInterface.ClientState.Login += OnLogin;
			DalamudInterface.ClientState.Logout += OnLogout;


			PluginUi = new PluginUI(this, Configuration);
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
				this.PluginUi.History.ShowHistory();
			}
			if (arg == "cfg" || arg == "config") { this.PluginUi.Setting.Show(); }
#if DEBUG
			else if (arg == "test") {
				Chat.PrintMsg("服务器id:" + homeWorldId);
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
			homeWorldId = DalamudInterface.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;
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

		private void OnLogout(object? sender, EventArgs e) {
			homeWorldId = 0;
		}
	}
}
