using Dalamud.Configuration;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

namespace TradeBuddy
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		private static readonly Dictionary<int, string> cnWorldDC = new();
		private static string dcName = "";
		public int Version { get; set; } = 0;

		/// <summary>
		/// 是否显示交易监控窗口
		/// </summary>
		public bool ShowTrade = true;

		/// <summary>
		/// 要求交易数量严格遵循某个预设单价的整数倍，否则尝试进行填充，暂时没用
		/// </summary>
		[NonSerialized]
		public bool StrictMode = true;
		[NonSerialized]
		public const int outOfTime = 30 * 1000;

		public string TradeConfirmStr = "交易完成。";
		public string TradeCancelStr = "交易取消。";
		public bool TradeConfirmAlert = true;
		public bool TradeCancelAlert = true;

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
		public RetainerSellList? sellList;
		public class RetainerSellList
		{
			[JsonPropertyName("ProperColor")]
			public int[] ProperColorArray => Config.RetainerSellListProperColor;

			[Newtonsoft.Json.JsonIgnore]
			public Vector3 ProperColor
			{
				get => new(ProperColorArray[0] / 255f, ProperColorArray[1] / 255f, ProperColorArray[2] / 255f);
				set
				{
					var list = new List<float>() { value.X, value.Z, value.Z };
					Config.RetainerSellListProperColor = list.Select(x => (int)Math.Round(x * 255)).ToArray();
				}
			}
			[JsonPropertyName("AlertColor")]
			public int[] AlertColorArray => Config.RetainerSellListAlertColor;

			[Newtonsoft.Json.JsonIgnore]
			public Vector3 AlertColor
			{
				get => new(AlertColorArray[0] / 255f, AlertColorArray[1] / 255f, AlertColorArray[2] / 255f);
				set
				{
					var list = new List<float>() { value.X, value.Z, value.Z };
					Config.RetainerSellListAlertColor = list.Select(x => (int)Math.Round(x * 255)).ToArray();
				}
			}
			[NonSerialized]
			public readonly static int[] Proper_Color_Default = new int[] { 10, 187, 10 };
			[NonSerialized]
			public readonly static int[] Alert_Color_Default = new int[] { 213, 28, 47 };
			[NonSerialized]
			public readonly static int[] Color_Default = new int[] { 0xCC, 0xCC, 0xCC };
			[NonSerialized]
			public Configuration Config;
			public RetainerSellList(Configuration config)
			{
				Config = config;
			}

		}

		public List<PresetItem> PresetItemList = new();

		[NonSerialized]
		public Dictionary<string, int> PresetItemDictionary = new();
		[NonSerialized]
		private readonly static Dictionary<uint, TextureWrap?> iconList = new();
		[NonSerialized]
		private readonly static Dictionary<uint, TextureWrap?> hqIconList = new();

		public class PresetItem
		{
			[Newtonsoft.Json.JsonIgnore]
			public uint itemId { get; private set; } = 0;

			private string itemName = "";
			public string ItemName { get => itemName; set { if (value != itemName) { itemName = value; Init(); } } }

			[Newtonsoft.Json.JsonIgnore]
			public uint iconId { get; private set; } = 0; // 0未找到

			[Newtonsoft.Json.JsonIgnore]
			public bool isHQ { get; private set; } = false;

			/// <summary>
			/// Universalis上面获取到的价格
			/// -1 获取失败
			/// 0 获取中
			/// </summary>
			[Newtonsoft.Json.JsonIgnore]
			public int minPrice { get; private set; } = -2;

			[Newtonsoft.Json.JsonIgnore]
			public string minPriceServer { get; private set; } = "";

			[Newtonsoft.Json.JsonIgnore]
			public long minPriceUpdateTime { get; private set; } = 0;
			public string GetMinPriceUpdateTimeStr() => DateTimeOffset.FromUnixTimeMilliseconds(minPriceUpdateTime).LocalDateTime.ToString();

			/// <summary>
			/// 价格规则，数量 / 价格
			/// </summary>
			public Dictionary<int, int> PriceList = new();

			private float evaluatePrice = 0;
			/// <summary>
			/// 单个物品评估价格，用于雇员板子显示、板子价格显示
			/// </summary>
			public float EvaluatePrice()
			{
				if (PriceList.Count == 0) evaluatePrice = 0;
				else if (evaluatePrice == 0)
				{
					evaluatePrice = 0;
					if (PriceList.Count > 0)
					{
						foreach (KeyValuePair<int, int> pair in PriceList)
						{
							if (evaluatePrice == 0 || pair.Value * 1.0f / pair.Key < evaluatePrice)
								evaluatePrice = pair.Value * 1.0f / pair.Key;
						}
					}
				}
				return evaluatePrice;
			}

			public string GetPriceStr()
			{
				if (PriceList.Count == 1 && PriceList.ContainsKey(1)) return Convert.ToString(PriceList[1]);

				return string.Join(';', PriceList.Select(pair => $"{pair.Value:#,##0}/{pair.Key:#,##0}"));
			}
			public void SetPriceStr(string value)
			{
#if DEBUG
				DalamudDll.ChatGui.Print("设置价格规则：" + value);
#endif
				string[] pricePairStr = value.Trim().Split(';');
				if (pricePairStr.Length == 1 && !pricePairStr[0].Trim().Contains('/'))
				{
					pricePairStr[0] += "/1";
				}
				if (pricePairStr.Length > 0) PriceList.Clear();
				foreach (string pricePair in pricePairStr)
				{
					string[] priceStr = pricePair.Trim().Split('/');
					if (priceStr.Length == 2)
					{
						try
						{
							int price = int.Parse(priceStr[0]);
							int count = int.Parse(priceStr[1]);
							PriceList.Add(count, price);
						}
						catch (FormatException e)
						{
							PluginLog.Error("从str解析预设价格失败，价格格式错误：" + value + "\n" + e.ToString());
						}
					}
				}
				EvaluatePrice();

			}
			public static int Sort(int x, int y)
			{
				if (x > y) return -1;
				else if (x < y) return 1;
				return 0;
			}

			/// <summary>
			/// 获取联网价格
			/// </summary>
			public void UpdateMinPrice()
			{
				minPriceServer = "";
				minPrice = 0;
				minPriceUpdateTime = -1;

				if (DalamudDll.ClientState.LocalContentId != 0 && string.IsNullOrEmpty(dcName))
				{
					if (!DalamudDll.ClientState.IsLoggedIn) return;
					dcName = GetWorldName();
				}
				if (string.IsNullOrEmpty(dcName))
				{
					minPrice = -1;
					//考虑一下要不要获取当前服务器而不是大区的价格
					//itemId, currentWorld.Name
				}
				else
				{
					Universalis.Client.GetCurrentlyShownView(dcName, itemId, price =>
					{
						if (price == null)
							minPrice = -1;
						else
						{
							minPriceUpdateTime = price.lastUploadTime;
							minPrice = isHQ ? price.minPriceHQ : price.minPriceNQ;
							minPriceServer = price.listings?[0].worldName ?? "";
						}
					});
				}

			}

			public static PresetItem ParseFromString(string str)
			{
				PresetItem presetItem = new();
				string[] itemStr = str.Replace("\r", "").Replace("\t", "").Trim().Split(':');
				if (itemStr.Length == 2)
				{
					presetItem.ItemName = itemStr[0];
					presetItem.SetPriceStr(itemStr[1]);
				}
				return presetItem;
			}

			public void Init()
			{
				if (ItemName.EndsWith("HQ"))
				{
					isHQ = true;
					itemName = itemName[0..^2];
				}
				var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == itemName);
				if (itemByName == null)
				{
					itemId = 0;
					iconId = 0;
				}
				else
				{
					itemId = itemByName.RowId;
					iconId = itemByName.Icon;
					if (!isHQ && !iconList.ContainsKey(iconId)) GetIcon(iconId, isHQ);
					if (isHQ && !hqIconList.ContainsKey(iconId)) GetIcon(iconId, isHQ);
					if (DalamudDll.ClientState.IsLoggedIn) UpdateMinPrice();
				}
			}

			public override string ToString() => $"{itemName}:{GetPriceStr()}";
		}

		public static TextureWrap? GetIcon(uint iconId) => GetIcon(iconId, false);
		public static TextureWrap? GetIcon(uint iconId, bool isHq)
		{
			if (iconId < 1) return null;
			if (!isHq && iconList.ContainsKey(iconId)) return iconList[iconId];
			if (isHq && hqIconList.ContainsKey(iconId)) return hqIconList[iconId];
			TextureWrap? icon = isHq ?
				DalamudDll.DataManager.GetImGuiTextureHqIcon(iconId) :
				DalamudDll.DataManager.GetImGuiTextureIcon(iconId);
			if (isHq)
				hqIconList.Add(iconId, icon);
			else
				iconList.Add(iconId, icon);

			return icon;
		}

		public void Dispose()
		{
			DalamudDll.ClientState.Login -= OnLogin;
			DalamudDll.ClientState.Logout -= OnLogout;
			foreach (TextureWrap? icon in iconList.Values) if (icon != null) icon.Dispose();
			foreach (TextureWrap? icon in hqIconList.Values) if (icon != null) icon.Dispose();
		}

		public void OnLogin(object? sender, EventArgs e)
		{
			PresetItemList.ForEach(item => item.UpdateMinPrice());
		}

		public void OnLogout(object? sender, EventArgs e)
		{
			dcName = "";
		}

		private void RefreshKeySet()
		{
			PresetItemDictionary.Clear();
			PresetItem[] list = PresetItemList.ToArray();
			for (int i = 0; i < list.Length; i++)
			{
				if (!PresetItemDictionary.ContainsKey(list[i].ItemName))
				{
					PresetItemDictionary.Add(list[i].ItemName, i);
				}
			}
		}

		#region Init and Save

		[NonSerialized]
		private DalamudPluginInterface? pluginInterface;

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			this.pluginInterface = pluginInterface;
			sellList = new RetainerSellList(this);
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
			DalamudDll.ClientState.Login += OnLogin;
			DalamudDll.ClientState.Logout += OnLogout;

			PresetItemList.ForEach(item => item.UpdateMinPrice());

			RefreshKeySet();
		}

		public static string GetWorldName()
		{
			var currentWorld = DalamudDll.ClientState.LocalPlayer?.CurrentWorld.GameData;
			if (currentWorld == null) return "";
			if (currentWorld.DataCenter.Value?.RowId != 0)
				return currentWorld.DataCenter.Value?.Name ?? "";
			else if (cnWorldDC.ContainsKey((int)currentWorld.RowId))
				return cnWorldDC[(int)currentWorld.RowId];
			return "";
		}
		public void Save()
		{
			this.pluginInterface!.SavePluginConfig(this);
			RefreshKeySet();
		}

		#endregion
	}
}
