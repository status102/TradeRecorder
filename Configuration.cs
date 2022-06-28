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
		public bool TradeConfirmAlert { get; set; } = false;
		public bool TradeCancelAlert { get; set; } = true;

		public string tradeConfirmStr { get; set; } = "交易完成。";
		public string tradeCancelStr { get; set; } = "交易取消。";

		public List<PresetItem> presetItemList = new();

		[NonSerialized]
		public Dictionary<string, int> presetItemDictionary = new();
		[NonSerialized]
		public static Dictionary<uint, TextureWrap?> iconList = new();
		[NonSerialized]
		public static Dictionary<uint, TextureWrap?> hqiconList = new();
		public class PresetItem
		{
			[NonSerialized]
			public int iconId = -1; // -1未初始化 0未找到
			[NonSerialized]
			public bool isHQ = false;// 仅用作加载icon
			public float price;
			public string name = "";

			public override string ToString() => String.Format("{0:},{1:}", name, price);

		}

		public static TextureWrap? GetIcon(uint iconId, bool isHq)
		{
			if (!isHq && iconList.ContainsKey(iconId)) return iconList[iconId];
			if (isHq && hqiconList.ContainsKey(iconId)) return hqiconList[iconId];
			TextureWrap? icon = isHq ?
				DalamudDll.DataManager.GetImGuiTextureHqIcon(iconId) :
				DalamudDll.DataManager.GetImGuiTextureIcon(iconId);
			if (isHq)
			{
				hqiconList.Add(iconId, icon);
			}
			else
			{
				iconList.Add(iconId, icon);
			}
			return icon;
		}

		public void Dispose()
		{
			foreach (TextureWrap? icon in iconList.Values) if (icon != null) icon.Dispose();
			foreach (TextureWrap? icon in hqiconList.Values) if (icon != null) icon.Dispose();
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
			presetItemDictionary.Clear();
			PresetItem[] list = presetItemList.ToArray();
			for (int i = 0; i < list.Length; i++)
			{
				if (!presetItemDictionary.ContainsKey(list[i].name))
				{
					presetItemDictionary.Add(list[i].name, i);
				}
			}
		}

		public void Save()
		{
			this.pluginInterface!.SavePluginConfig(this);
		}

		#endregion
	}
}
