using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiScene;
using System;
using System.Collections.Generic;

namespace TradeBuddy
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; } = 0;

		public bool ShowTrade { get; set; } = true;
		public bool PrintConfirmTrade { get; set; } = false;
		public bool PrintCancelTrade { get; set; } = true;

		public string tradeConfirmStr { get; set; } = "交易完成。";
		public string tradeCancelStr { get; set; } = "交易取消。";

		public List<PresetItem> presetList = new List<PresetItem>();

		[NonSerialized]
		public Dictionary<string, int> presetItem = new Dictionary<string, int>();
		[NonSerialized]
		public static Dictionary<uint, TextureWrap?> iconList = new Dictionary<uint, TextureWrap?>();
		[NonSerialized]
		public static Dictionary<uint, TextureWrap?> hqiconList = new Dictionary<uint, TextureWrap?>();
		public class PresetItem
		{
			[NonSerialized]
			public int iconId = -1; // -1未初始化 0未找到
			[NonSerialized]
			public bool isHQ = false;// 仅用作加载icon
			public int price;
			public string name = "";
		}

		public static TextureWrap? getIcon(uint iconId, bool isHq)
		{
			if (!isHq && iconList.ContainsKey(iconId)) return iconList[iconId];
			if (isHq && hqiconList.ContainsKey(iconId)) return hqiconList[iconId];
			TextureWrap? icon = isHq ?
				DalamudDll.DataManager.GetImGuiTextureHqIcon(iconId) :
				DalamudDll.DataManager.GetImGuiTextureIcon(iconId);
			if (isHq)
				hqiconList.Add(iconId, icon);
			else
				iconList.Add(iconId, icon);
			return icon;
		}

		public void Dispose()
		{
			foreach(TextureWrap? icon in iconList.Values)if(icon != null)icon.Dispose();
			foreach(TextureWrap? icon in hqiconList.Values)if(icon != null)icon.Dispose();
		}

		#region Init and Save

		[NonSerialized]
		private DalamudPluginInterface? pluginInterface;

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			this.pluginInterface = pluginInterface;
			RefreshKeySet();
		}

		public void RefreshKeySet()
		{
			presetItem.Clear();
			PresetItem[] list = presetList.ToArray();
			for (int i = 0; i < list.Length; i++)
				if(!presetItem.ContainsKey(list[i].name)) presetItem.Add(list[i].name, i);
		}

		public void Save()
		{
			this.pluginInterface!.SavePluginConfig(this);
		}

		#endregion
	}
}
