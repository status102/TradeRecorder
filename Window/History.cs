using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace TradeBuddy.Window
{
	public class History : IDisposable
	{

		public class TradeHistory
		{
			public bool visible = true;//控制显示

			private readonly static string TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
			private readonly static char PART_SPLIT = ';';
			private readonly static char ITEM_SPLIT = ',';
			private readonly static char COUNT_SPLIT = 'x';
			public bool isSuccess = true;
			public string time = DateTime.Now.ToString(TIME_FORMAT);
			public string targetName = "";
			public int giveGil = 0, receiveGil = 0;
			public Item[] giveItemArray = Array.Empty<Item>(), receiveItemArray = Array.Empty<Item>();

			public class Item
			{
				public string name;
				public int count;
				private ushort iconId = 0;
				private bool isHQ;

				public TextureWrap? icon { get; private set; }

				public Item(string name, int count)
				{
					this.name = name;
					this.count = count;
					isHQ = name.EndsWith("HQ");
					var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == name.Replace("HQ", String.Empty));
					if (itemByName != null)
						iconId = itemByName.Icon;
					if (iconId > 0) icon = Configuration.GetIcon(iconId, isHQ);
				}
				public override string ToString() => $"{name}{COUNT_SPLIT}{count}";
				public string ToShowString() => $"{name} {COUNT_SPLIT} {count}";
			}
			public static TradeHistory? ParseFromString(string str)
			{
				string[] strArray = str.Split(PART_SPLIT);
				if (strArray.Length != 7) return null;

				TradeHistory tradeHistory = new()
				{
					time = strArray[0],
					isSuccess = bool.Parse(strArray[1]),
					targetName = strArray[2],
					giveGil = int.Parse(strArray[3]),
					receiveGil = int.Parse(strArray[4]),
					giveItemArray = strArray[5].Split(ITEM_SPLIT).Select(i => i.Split(COUNT_SPLIT)).Where(i => i.Length == 2).Select(i => new Item(i[0], int.Parse(i[1]))).ToArray(),
					receiveItemArray = strArray[6].Split(ITEM_SPLIT).Select(i => i.Split(COUNT_SPLIT)).Where(i => i.Length == 2).Select(i => new Item(i[0], int.Parse(i[1]))).ToArray()
				};

				return tradeHistory;
			}
			public override string ToString()
			{
				List<string> write = new()
				{
					time,Convert.ToString(isSuccess),targetName,
					Convert.ToString(giveGil),Convert.ToString(receiveGil),
					string.Join<Item>(ITEM_SPLIT, giveItemArray),
					string.Join<Item>(ITEM_SPLIT, receiveItemArray)
				};
				return string.Join(PART_SPLIT, write);
			}
		}

		/// <summary>
		/// 删除记录后重新去文件读取
		/// </summary>
		private bool _refresh = true, _edit = false;
		private static readonly Vector2 Img_Size = new(26, 26);
		private List<TradeHistory> _tradeHistoryList = new();
		//private string playerName = "", playerWorld = "";

		private TradeBuddy tradeBuddy;

		public History(TradeBuddy tradeBuddy)
		{
			this.tradeBuddy = tradeBuddy;
			Task.Run(() => ReadHistory());
		}

		public void DrawHistory(ref bool historyVisible)
		{
			if (!historyVisible)
			{
				if (_edit)
				{
					_edit = false;
					_refresh = false;
					Task.Run(() => { EditHistory(); ReadHistory(); });
				}
				return;
			}

			if (_refresh) { _refresh = false; Task.Run(() => ReadHistory()); }

			ImGui.SetNextWindowSize(new Vector2(480, 600), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("交易历史记录", ref historyVisible, ImGuiWindowFlags.NoScrollbar))
			{
				if (ImGui.Button("全部清除")) ClearHistory();

				if (ImGui.BeginChild("##历史清单"))
				{
					for (int index = 0; index < _tradeHistoryList.Count; index++)
					{
						TradeHistory tradeItem = _tradeHistoryList[index];

						var title = $"{index + 1}:  {tradeItem.time}  <{tradeItem.targetName}>";
						if (!tradeItem.isSuccess) title += "  (取消)";
						if (ImGui.CollapsingHeader(title.ToString(), ref tradeItem.visible))
						{
							if (ImGui.BeginTable("histroy", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
							{
								ImGui.TableSetupColumn("支付");
								ImGui.TableSetupColumn("接收");
								ImGui.TableHeadersRow();
								for (int i = 0; i < Math.Max(tradeItem.giveItemArray.Length, tradeItem.receiveItemArray.Length); i++)
								{
									ImGui.TableNextRow();
									ImGui.TableNextColumn();

									if (tradeItem.giveItemArray.Length > i)
									{
										var icon = tradeItem.giveItemArray[i].icon;
										if (icon != null)
										{
											ImGui.Image(icon.ImGuiHandle, Img_Size);
											ImGui.SameLine();
										}
										ImGui.TextUnformatted(tradeItem.giveItemArray[i].ToShowString());
									}

									ImGui.TableNextColumn();
									if (tradeItem.receiveItemArray.Length > i)
									{
										var icon = tradeItem.receiveItemArray[i].icon;
										if (icon != null)
										{
											ImGui.Image(icon.ImGuiHandle, Img_Size);
											ImGui.SameLine();
										}
										ImGui.TextUnformatted(tradeItem.receiveItemArray[i].ToShowString());
									}

								}
								if (tradeItem.giveGil > 0 || tradeItem.receiveGil > 0)
								{
									ImGui.TableNextRow();
									ImGui.TableNextColumn();
									if (tradeItem.giveGil > 0) ImGui.TextUnformatted(($"金币: {tradeItem.giveGil:#,##0}" ));

									ImGui.TableNextColumn();
									if (tradeItem.receiveGil > 0) ImGui.TextUnformatted($"金币: {tradeItem.receiveGil:#,##0}");
								}

								ImGui.EndTable();
							}
						}
						//删除记录
						if (!tradeItem.visible) _edit = true;

					}
				}
			}

		}

		public void PushTradeHistory(string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemArray, List<KeyValuePair<string, int>> receiveItemArray) => PushTradeHistory(true, targetName, giveGil, receiveGil, giveItemArray, receiveItemArray);

		public void PushTradeHistory(bool isSuccess, string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemList, List<KeyValuePair<string, int>> receiveItemList)
		{
			var playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			List<TradeHistory.Item> giveList = new(), receiviList = new();
			giveItemList.ForEach(i => giveList.Add(new(i.Key, i.Value)));
			receiveItemList.ForEach(i => receiviList.Add(new(i.Key, i.Value)));

			TradeHistory tradeHistory = new()
			{
				isSuccess = isSuccess,
				targetName = targetName,
				giveGil = giveGil,
				receiveGil = receiveGil,
				giveItemArray = giveList.ToArray(),
				receiveItemArray = receiviList.ToArray()
			};

			using (FileStream stream = File.Open(Path.Join(tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Append))
			{
				StreamWriter writer = new(stream);
				writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			_refresh = true;
		}

		public void ReadHistory()
		{
			if (DalamudDll.ClientState.LocalPlayer == null) return;
			var playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			_tradeHistoryList = new();

			string path = Path.Join(tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

			using (StreamReader reader = new(File.Open(path, FileMode.OpenOrCreate)))
			{
				string tradeStr;
				while ((tradeStr = reader.ReadLine() ?? "").Length > 0)
				{
					TradeHistory? trade = TradeHistory.ParseFromString(tradeStr);
					if (trade != null) _tradeHistoryList.Add(trade);
				}
			}
			_refresh = false;
		}

		public void EditHistory()
		{
			if (DalamudDll.ClientState.LocalPlayer == null) return;
			var playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			using (FileStream stream = File.Open(Path.Join(tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create))
			{
				StreamWriter writer = new(stream);
				foreach (TradeHistory tradeHistory in _tradeHistoryList)
					if (tradeHistory.visible) writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			_edit = false;
		}

		public void ClearHistory()
		{
			if (DalamudDll.ClientState.LocalPlayer == null) return;
			var playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			File.Delete(Path.Join(tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"));
		}

		public void Dispose()
		{
			_tradeHistoryList.Clear();
		}
	}
}
