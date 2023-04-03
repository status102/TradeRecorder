using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using TradeBuddy.Model;

namespace TradeBuddy
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		[Newtonsoft.Json.JsonIgnore]
		private static TradeBuddy? TradeBuddy;
		private static readonly Dictionary<int, string> cnWorldDC = new();
		public static string dcName = "";
		public int Version { get; set; } = 0;

		/// <summary>
		/// 是否显示交易监控窗口
		/// </summary>
		public bool ShowTrade = true;

		public string TradeConfirmStr = "交易完成。";
		public string TradeCancelStr = "交易取消。";
		public bool TradeConfirmAlert = true;
		public bool TradeCancelAlert = true;

		/// <summary>
		/// 判定当前是否处于交易中
		/// </summary>
		public ushort OpcodeOfTradeForm = 594;// 需要维护
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

		/// <summary>
		/// 雇员出售价格合适时，修改单价颜色
		/// </summary>
		public bool DrawRetainerSellListProper = true;
		/// <summary>
		/// 雇员出售价格过低时，修改单价颜色
		/// </summary>
		public bool DrawRetainerSellListAlert = true;
		/// <summary>
		/// 雇员出售列表，价格合适自定义颜色
		/// </summary>
		[JsonPropertyName("RetainerSellListProperColor")]
		public int[] RetainerSellListProperColor = new int[] { 10, 187, 10 };
		/// <summary>
		/// 雇员出售列表，价格过低自定义颜色
		/// </summary>
		[JsonPropertyName("RetainerSellListAlertColor")]
		public int[] RetainerSellListAlertColor = new int[] { 213, 28, 47 };


		[JsonPropertyName("RetainerSellList")]
		public RetainerSellList SellList;

		public List<PresetItem> PresetItemList = new();

		[NonSerialized]
		public Dictionary<string, int> PresetItemDictionary = new();


		public class RetainerSellList
		{
			[JsonPropertyName("ProperColor")]
			public int[] ProperColorArray => Config.RetainerSellListProperColor;

			[Newtonsoft.Json.JsonIgnore]
			public Vector3 ProperColor {
				get => new(ProperColorArray[0] / 255f, ProperColorArray[1] / 255f, ProperColorArray[2] / 255f);
				set => Config.RetainerSellListProperColor = new List<float>() { value.X, value.Y, value.Z }.Select(x => (int)Math.Round(x * 255)).ToArray();
			}
			[JsonPropertyName("AlertColor")]
			public int[] AlertColorArray => Config.RetainerSellListAlertColor;

			[Newtonsoft.Json.JsonIgnore]
			public Vector3 AlertColor {
				get => new(AlertColorArray[0] / 255f, AlertColorArray[1] / 255f, AlertColorArray[2] / 255f);
				set => Config.RetainerSellListAlertColor = new List<float>() { value.X, value.Y, value.Z }.Select(x => (int)Math.Round(x * 255)).ToArray();
			}
			[NonSerialized]
			public readonly static int[] Proper_Color_Default = { 10, 187, 10 };
			[NonSerialized]
			public readonly static int[] Alert_Color_Default = { 213, 28, 47 };
			[NonSerialized]
			public readonly static int[] Color_Default = { 0xCC, 0xCC, 0xCC };
			[NonSerialized]
			public Configuration Config;
			public RetainerSellList(Configuration config) {
				Config = config;
			}

		}

		public void Dispose() {		}

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
			Configuration.TradeBuddy = TradeBuddy;
			SellList = new RetainerSellList(this);

			#region 追加服务器ID
			cnWorldDC.Add(1167, "LuXingNiao");
			cnWorldDC.Add(1081, "LuXingNiao");
			cnWorldDC.Add(1042, "LuXingNiao");
			cnWorldDC.Add(1044, "LuXingNiao");
			cnWorldDC.Add(1060, "LuXingNiao");
			cnWorldDC.Add(1173, "LuXingNiao");
			cnWorldDC.Add(1174, "LuXingNiao");
			cnWorldDC.Add(1175, "LuXingNiao");
			cnWorldDC.Add(1172, "MoGuLi");
			cnWorldDC.Add(1076, "MoGuLi");
			cnWorldDC.Add(1171, "MoGuLi");
			cnWorldDC.Add(1170, "MoGuLi");
			cnWorldDC.Add(1113, "MoGuLi");
			cnWorldDC.Add(1121, "MoGuLi");
			cnWorldDC.Add(1166, "MoGuLi");
			cnWorldDC.Add(1176, "MoGuLi");
			cnWorldDC.Add(1043, "MaoXiaoPang");
			cnWorldDC.Add(1169, "MaoXiaoPang");
			cnWorldDC.Add(1106, "MaoXiaoPang");
			cnWorldDC.Add(1045, "MaoXiaoPang");
			cnWorldDC.Add(1177, "MaoXiaoPang");
			cnWorldDC.Add(1178, "MaoXiaoPang");
			cnWorldDC.Add(1179, "MaoXiaoPang");
			cnWorldDC.Add(1192, "DouDouChai");
			cnWorldDC.Add(1183, "DouDouChai");
			cnWorldDC.Add(1180, "DouDouChai");
			cnWorldDC.Add(1186, "DouDouChai");
			cnWorldDC.Add(1193, "DouDouChai");
			cnWorldDC.Add(1068, "DouDouChai");
			cnWorldDC.Add(1064, "DouDouChai");
			cnWorldDC.Add(1187, "DouDouChai");
			#endregion

			RefreshKeySet();
		}

		public static string GetWorldName() {
			var currentWorld = TradeBuddy?.ClientState.LocalPlayer?.CurrentWorld.GameData;
			if (currentWorld == null)
				return "";
			if (currentWorld.DataCenter.Value?.RowId != 0)
				return currentWorld.DataCenter.Value?.Name ?? "";
			else if (cnWorldDC.ContainsKey((int)currentWorld.RowId))
				return cnWorldDC[(int)currentWorld.RowId];
			return "";
		}
		public void Save() {
			this.pluginInterface!.SavePluginConfig(this);
			RefreshKeySet();
		}

		#endregion
	}
}
