using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBuddy.Model
{
	public class TradeHistory
	{
		private const string TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
		private const char PART_SPLIT = ';';
		private const char ITEM_SPLIT = ',';
		private const char COUNT_SPLIT = 'x';
		
		public string time { get; init; } = DateTime.Now.ToString(TIME_FORMAT);
		/// <summary>
		/// 是否显示该项目
		/// </summary>
		public bool visible = true;
		public bool isSuccess { get; init; } = true;
		public string targetName { get; init; } = "";
		public int giveGil { get; init; } = 0;
		public int receiveGil { get; init; } = 0;
		public HistoryItem[] giveItemArray { get; init; } = Array.Empty<HistoryItem>();
		public HistoryItem[] receiveItemArray { get; init; } = Array.Empty<HistoryItem>();

		public class HistoryItem
		{
			public string name { get; init; }
			public int count { get; init; }
			private ushort iconId = 0;
			private bool isHQ = false;
			public TextureWrap? icon { get; init; }

			public HistoryItem(string name, int count) {
				this.name = name;
				this.count = count;
				isHQ = name.EndsWith("HQ");
				var itemByName = TradeBuddy.Instance?.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == name.Replace("HQ", String.Empty));
				if (itemByName != null)
					iconId = itemByName.Icon;
				if (iconId > 0)
					icon = TradeBuddy.Instance?.GetIcon(iconId, isHQ);
			}
			public override string ToString() => $"{name}{COUNT_SPLIT}{count}";
			public string ToShowString() => $"{name} {COUNT_SPLIT} {count}";
		}
		public static TradeHistory? ParseFromString(string str) {
			string[] strArray = str.Split(PART_SPLIT);
			if (strArray.Length != 7)
				return null;

			TradeHistory tradeHistory = new()
			{
				time = strArray[0],
				isSuccess = bool.Parse(strArray[1]),
				targetName = strArray[2],
				giveGil = int.Parse(strArray[3]),
				receiveGil = int.Parse(strArray[4]),
				giveItemArray = strArray[5].Split(ITEM_SPLIT).Select(i => i.Split(COUNT_SPLIT)).Where(i => i.Length == 2).Select(i => new HistoryItem(i[0], int.Parse(i[1]))).ToArray(),
				receiveItemArray = strArray[6].Split(ITEM_SPLIT).Select(i => i.Split(COUNT_SPLIT)).Where(i => i.Length == 2).Select(i => new HistoryItem(i[0], int.Parse(i[1]))).ToArray()
			};
			return tradeHistory;
		}
		public override string ToString() {
			List<string> write = new()
				{
					time,Convert.ToString(isSuccess),targetName,
					Convert.ToString(giveGil),Convert.ToString(receiveGil),
					string.Join<HistoryItem>(ITEM_SPLIT, giveItemArray),
					string.Join<HistoryItem>(ITEM_SPLIT, receiveItemArray)
				};
			return string.Join(PART_SPLIT, write);
		}
	}
}
