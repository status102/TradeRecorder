using Dalamud.Logging;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TradeBuddy.Model
{
	public class TradeItem
	{
		public TextureWrap? icon;
		public uint iconId = 0;
		public string name = "";
		public bool isHQ = false;
		public int count = 0;
		public int price = 0;
		/// <summary>
		/// 0-默认， 1-设定HQ交易NQ， 2-设定NQ交易HQ
		/// </summary>
		public byte priceType = 0;
		/// <summary>
		/// 所匹配的预设名称
		/// </summary>
		public string priceName = "";
		/// <summary>
		/// 联网获取最低价，-2未初始化，-1获取失败，0获取中
		/// </summary>
		public int minPrice { get; private set; } = -2;
		public string minPriceServer { get; private set; } = "";

		public Dictionary<int, int> priceList = new();

		public string GetMinPriceStr() =>
			 minPrice switch
			 {
				 -1 => "获取失败",
				 0 => "获取中",
				 _ => $"{minPrice:#,0}"
			 };


		public void UpdateMinPrice() {

			minPrice = 0;
			uint itemId = 0;
			string worldName = Configuration.GetWorldName();
			var itemByName = Dalamud.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == (isHQ ? name[0..^2] : name));
			if (itemByName == null) {
				minPrice = -1;
			} else {
				itemId = itemByName.RowId;
				if (itemId > 1 && !string.IsNullOrEmpty(worldName)) {
					Task.Run(async () =>
					{
						try {
							var price = await Universalis.API.UniversalisClient.GetMarketData(worldName, itemId, CancellationToken.None);
							if (price == null || price.itemID != itemId)
								minPrice = -1;
							else {
								if (isHQ)
									minPrice = price.minPriceHQ;
								else if (price.minPriceNQ > 0 && price.minPriceHQ > 0)
									minPrice = Math.Min(price.minPriceNQ, price.minPriceHQ);
								else
									minPrice = price.minPriceNQ;
								minPriceServer = price.listings?[0].worldName ?? "";
							}
						} catch (HttpRequestException e) {
							PluginLog.Error(e.ToString());
							minPrice = -1;
						}
					});
				} else
					minPrice = -1;
			}
		}
	}
}
