using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace TradeBuddy
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; } = 0;

		public bool ShowTrade { get; set; } = true;
		public bool PrintConfirmTrade { get; set; } = true;
		public bool PrintCancelTrade { get; set; } = true;

		public string tradeConfirmStr { get; set; } = "交易完成。";
		public string tradeCancelStr { get; set; } = "交易取消。";

		public List<PresetItem> presetList = new List<PresetItem>();

		[NonSerialized]
		public Dictionary<string, int> presetItem = new Dictionary<string, int>();
		public class PresetItem
		{
			public int price;
			public string name = "";
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
				presetItem.Add(list[i].name, i);
		}

		public void Save()
		{
			this.pluginInterface!.SavePluginConfig(this);
		}

		#endregion
	}
}
