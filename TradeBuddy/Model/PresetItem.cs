using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TradeBuddy.Model
{

    public class PresetItem
	{

		[Newtonsoft.Json.JsonIgnore]
		public uint iconId { get; private set; } = 0; // 0未找到

		[Newtonsoft.Json.JsonIgnore]
		public bool quality { get; private set; } = false;

		[Newtonsoft.Json.JsonIgnore]
		public uint itemId { get; private set; } = 0;

		private string itemName = "";
		public string ItemName { get => itemName; set { if (value != itemName) { itemName = value; Init(); } } }


		/// <summary>
		/// Universalis上面获取到的价格
		/// -1 获取失败
		/// 0 获取中
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public int minPrice { get; private set; } = -2;

		public string GetMinPriceStr(bool addServer)
			=> minPrice switch
			{
				-1 => "获取失败",
				0 => "获取中",
				_ => (addServer ? $"<{minPriceServer}>" : "") + $"{minPrice:#,0}"
			};

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
		public float EvaluatePrice() {
			if (PriceList.Count == 0)
				evaluatePrice = 0;
			else if (evaluatePrice == 0) {
				evaluatePrice = 0;
				if (PriceList.Count > 0) {
					foreach (KeyValuePair<int, int> pair in PriceList) {
						if (evaluatePrice == 0 || pair.Value * 1.0f / pair.Key < evaluatePrice)
							evaluatePrice = pair.Value * 1.0f / pair.Key;
					}
				}
			}
			return evaluatePrice;
		}

		public string GetPriceStr() {
			if (PriceList.Count == 1 && PriceList.ContainsKey(1))
				return Convert.ToString(PriceList[1]);

			return string.Join(';', PriceList.Select(pair => $"{pair.Value:#,0}/{pair.Key:#,0}"));
		}
		public void SetPriceStr(string value) {
			string[] pricePairStr = value.Trim().Split(';');
			if (pricePairStr.Length == 1 && !pricePairStr[0].Trim().Contains('/')) {
				pricePairStr[0] += "/1";
			}
			if (pricePairStr.Length > 0)
				PriceList.Clear();
			foreach (string pricePair in pricePairStr) {
				string[] priceStr = pricePair.Trim().Split('/');
				if (priceStr.Length == 2) {
					try {
						int price = int.Parse(priceStr[0]);
						int count = int.Parse(priceStr[1]);
						PriceList.Add(count, price);
					} catch (FormatException e) {
						PluginLog.Error("从str解析预设价格失败，价格格式错误：" + value + "\n" + e.ToString());
					}
				}
			}
			EvaluatePrice();

		}
		public static int Sort(int x, int y) {
			if (x > y)
				return -1;
			else if (x < y)
				return 1;
			return 0;
		}

		/// <summary>
		/// 获取联网价格
		/// </summary>
		public void UpdateMinPrice(Action? action = null) {
			// TODO 注释了获取价格
			/*
			minPriceServer = "";
			minPrice = 0;
			minPriceUpdateTime = -1;

			if (string.IsNullOrEmpty(Configuration.dcName)) {
				Configuration.dcName = Configuration.GetWorldName();
			}
			if (string.IsNullOrEmpty(Configuration.dcName)) {
				minPrice = -2;
				//考虑一下要不要获取当前服务器而不是大区的价格
				//itemId, currentWorld.Name
			} else {
				Task.Run(async () =>
				{
					try {
						var price = await Universalis.API.UniversalisClient.GetMarketData(Configuration.dcName, itemId, CancellationToken.None);
						if (price == null || price.itemID != itemId)
							minPrice = -1;
						else {
							minPriceUpdateTime = price.lastUploadTime;
							minPrice = quality ? price.minPriceHQ : price.minPriceNQ;
							minPriceServer = price.listings?[0].worldName ?? "";
						}
					} catch (HttpRequestException e) {
						PluginLog.Error(e.ToString());
						minPrice = -1;
					}
				});
			}*/
		}
		#region init
		public static PresetItem ParseFromString(string str) {
			PresetItem presetItem = new();
			string[] itemStr = str.Replace("\r", "").Replace("\t", "").Trim().Split(':');
			if (itemStr.Length == 2) {
				presetItem.ItemName = itemStr[0];
				presetItem.SetPriceStr(itemStr[1]);
			}
			return presetItem;
		}

		public void Init() {
			if (ItemName.EndsWith("HQ")) {
				quality = true;
				itemName = itemName[0..^2];
			}
			var itemByName = DalamudInterface.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == itemName);
			if (itemByName == null) {
				itemId = 0;
				iconId = 0;
			} else {
				itemId = itemByName.RowId;
				iconId = itemByName.Icon;
				if (TradeBuddy.Instance?.ClientState.IsLoggedIn ?? false)
					UpdateMinPrice();
			}
		}
		#endregion
		public override string ToString() => $"{itemName}:{GetPriceStr()}";
	}
}
