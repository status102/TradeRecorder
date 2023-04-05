using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using TradeBuddy.Model;

namespace TradeBuddy
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; } = 0;

		/// <summary>
		/// 是否显示交易监控窗口
		/// </summary>
		public bool ShowTrade = true;

		/// <summary>
		/// 判定当前是否处于交易中
		/// </summary>
		public ushort OpcodeOfTradeForm = 594;// 需要维护
		/// <summary>
		/// 交易开始前，发送交易双方ID
		/// </summary>
		public ushort OpcodeOfTradeTargetInfo = 458;// 需要维护
		/// <summary>
		/// 己方物品栏
		/// </summary>
		public ushort OpcodeOfInventoryModifyHandler = 762;
		/// <summary>
		/// 对方物品栏的物品变化
		/// </summary>
		public ushort OpcodeOfItemInfo = 921;
		/// <summary>
		/// 对方物品栏的水晶、金币
		/// </summary>
		public ushort OpcodeOfCurrencyCrystalInfo = 851;
		/// <summary>
		/// 交易成功后会触发该包，但是如果没有交易任何物品，则不会触发
		/// </summary>
		public ushort OpcodeOfUpdateInventorySlot = 0x0313;


		public List<PresetItem> PresetItemList = new();

		[NonSerialized]
		public Dictionary<string, int> PresetItemDictionary = new();

		public void Dispose() { }

		private void RefreshKeySet() {
			PresetItemDictionary.Clear();
			PresetItem[] list = PresetItemList.ToArray();
			for (int i = 0; i < list.Length; i++) {
				if (!PresetItemDictionary.ContainsKey(list[i].ItemName)) {
					PresetItemDictionary.Add(list[i].ItemName, i);
				}
			}
		}

		#region Init and Save

		[NonSerialized]
		private DalamudPluginInterface? pluginInterface;

		public void Initialize(TradeBuddy TradeBuddy, DalamudPluginInterface pluginInterface) {
			this.pluginInterface = pluginInterface;

			RefreshKeySet();
		}
		public void Save() {
			this.pluginInterface!.SavePluginConfig(this);
			RefreshKeySet();
		}

		#endregion
	}
}
