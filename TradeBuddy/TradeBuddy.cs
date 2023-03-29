using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TradeBuddy
{
	public sealed class TradeBuddy : IDalamudPlugin
	{
		public string Name => "Trade Buddy";

		private const string commandName = "/tb";

		private readonly static Dictionary<uint, TextureWrap?> iconList = new();
		private readonly static Dictionary<uint, TextureWrap?> hqIconList = new();
		public static TradeBuddy? Instance { get; private set; }
		public PluginUI PluginUi { get; init; }
		public Configuration Configuration { get; init; }

		#region 
		public DalamudPluginInterface PluginInterface { get; private set; }
		public CommandManager CommandManager { get; private set; }
		public DataManager DataManager { get; private set; }
		public ClientState ClientState { get; private set; }
		public GameNetwork GameNetwork { get; private set; }
		public ChatGui ChatGui { get; private set; }
		public GameGui GameGui { get; private set; }
		#endregion

		public TradeBuddy(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager,
			[RequiredVersion("1.0")] ClientState clientState,
			[RequiredVersion("1.0")] GameNetwork gameNetwork,
			[RequiredVersion("1.0")] DataManager dataManager,
			[RequiredVersion("1.0")] ChatGui chatGui,
			[RequiredVersion("1.0")] GameGui gameGui
		) {
			Instance = this;
			this.PluginInterface = pluginInterface;
			this.CommandManager = commandManager;
			this.ClientState = clientState;
			this.GameNetwork = gameNetwork;
			this.DataManager = dataManager;
			this.GameGui = gameGui;
			this.ChatGui = chatGui;

			this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			this.Configuration.Initialize(this, PluginInterface);

			//var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
			//var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
			this.PluginUi = new PluginUI(this, this.Configuration);

			commandManager.AddHandler(commandName, new CommandInfo(OnCommand)
			{
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
			this.PluginUi.Dispose();
			CommandManager.RemoveHandler(commandName);

			foreach (TextureWrap? icon in iconList.Values)
				icon?.Dispose();
			foreach (TextureWrap? icon in hqIconList.Values)
				icon?.Dispose();
		}

		private void OnCommand(string command, string args) {
			string arg = args.Trim().Replace("\"", string.Empty);
			if (string.IsNullOrEmpty(arg)) {
				this.PluginUi.historyVisible = !this.PluginUi.historyVisible;
			}
			if (arg == "cfg" || arg == "config")
				this.PluginUi.SettingsVisible = true;
			
		}

		private void DrawUI() {
			this.PluginUi.Draw();
		}

		private void DrawConfigUI() {
			this.PluginUi.SettingsVisible = !this.PluginUi.SettingsVisible;
		}



		private void OnLogin(object? sender, EventArgs e) {
			Action initAction = () =>
			{
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
				ChatGui.Print(output.ToString());
			};
			Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(_ =>
			{
				Configuration.dcName = Configuration.GetWorldName();
				if (Configuration.PresetItemList.Count == 0)
					return;
				int initCount = 0;

				Configuration.PresetItemList.ForEach((item) =>
				{
					item.UpdateMinPrice(() =>
					 {
						 if (++initCount >= Configuration.PresetItemList.Count)
							 initAction();
					 });
				});
			});
		}

		private void OnLogout(object? sender, EventArgs e) {
			Configuration.dcName = "";
		}

		//public TextureWrap? GetIcon(uint iconId) => GetIcon(iconId, false);
		public TextureWrap? GetIcon(uint iconId, bool isHq = false) {
			if (iconId < 1)
				return null;
			if (!isHq && iconList.ContainsKey(iconId))
				return iconList[iconId];
			if (isHq && hqIconList.ContainsKey(iconId))
				return hqIconList[iconId];
			/*
			TextureWrap? icon = isHq ?
				DataManager.GetImGuiTextureHqIcon(iconId) :
				DataManager.GetImGuiTextureIcon(iconId);*/
			TextureWrap? icon = GetIconStr(iconId, isHq);
			if (icon == null)
				return null;
			if (isHq)
				hqIconList.Add(iconId, icon);
			else
				iconList.Add(iconId, icon);

			return icon;
		}
		private TextureWrap? GetIconStr(uint iconId, bool isHQ) {
			//"ui/icon/{0:D3}000/{1}{2:D6}.tex";
			return DataManager.GetImGuiTexture(string.Format("ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex", iconId / 1000u, isHQ ? "hq/" : "", iconId));
		}
	}
}
