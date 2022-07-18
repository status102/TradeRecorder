using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace TradeBuddy.Window
{
	public class History
	{
		/// <summary>
		/// 删除记录后重新去文件读取
		/// </summary>
		private bool _refresh = true;
		private List<TradeHistory> tradeHistoryList = new();
		private string playerName = "", playerWorld = "";

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

				public Item(string name, int count)
				{
					this.name = name;
					this.count = count;
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

		public void DrawHistory(ref bool historyVisible)
		{
			if (!historyVisible || DalamudDll.ClientState.LocalPlayer == null) return;

			playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			if (_refresh)
			{
				tradeHistoryList = new();

				string path = Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

				using (StreamReader reader = new(File.Open(path, FileMode.OpenOrCreate)))
				{
					string tradeStr;
					while ((tradeStr = reader.ReadLine() ?? "").Length > 0)
					{
						TradeHistory? trade = TradeHistory.ParseFromString(tradeStr);
						if (trade != null) tradeHistoryList.Add(trade);
					}
				}
				_refresh = false;
			}
			ImGui.SetNextWindowSize(new Vector2(480, 600), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("交易历史记录", ref historyVisible))
			{
				if (ImGui.Button("全部清除"))
				{
					using StreamWriter writer = new(File.Open(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create));
					writer.Flush();
					_refresh = true;
				}

				for (int index = 0; index < tradeHistoryList.Count; index++)
				{
					TradeHistory tradeItem = tradeHistoryList[index];
					StringBuilder title = new();
					title.Append(index + 1).Append($":  {tradeItem.time}  <{tradeItem.targetName}>");
					if (!tradeItem.isSuccess) title.Append("  (取消)");
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

								// todo 历史记录增加图片
								//ImGui.SameLine();

								if (tradeItem.giveItemArray.Length > i)
								{
									var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == tradeItem.giveItemArray[i].name);
									if (itemByName != null)
									{
										ushort iconId = itemByName.Icon;
										var iconTexture = Configuration.GetIcon(iconId, tradeItem.giveItemArray[i].name.EndsWith("HQ"));
										if (iconTexture != null)
										{
											ImGui.Image(iconTexture.ImGuiHandle, new(20, 20));
											ImGui.SameLine();
										}

									}
									ImGui.TextUnformatted(tradeItem.giveItemArray[i].ToShowString());
								}

								ImGui.TableNextColumn();
								if (tradeItem.receiveItemArray.Length > i)
								{
									var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == tradeItem.receiveItemArray[i].name);
									if (itemByName != null)
									{
										ushort iconId = itemByName.Icon;
										var iconTexture = Configuration.GetIcon(iconId, tradeItem.receiveItemArray[i].name.EndsWith("HQ"));
										if (iconTexture != null)
										{
											ImGui.Image(iconTexture.ImGuiHandle, new(20, 20));
											ImGui.SameLine();
										}

									}
									ImGui.TextUnformatted(tradeItem.receiveItemArray[i].ToShowString());
								}

							}
							if (tradeItem.giveGil > 0 || tradeItem.receiveGil > 0)
							{
								ImGui.TableNextRow();
								ImGui.TableNextColumn();
								if (tradeItem.giveGil > 0) ImGui.TextUnformatted(string.Format("金币: {0:0,0}", tradeItem.giveGil).TrimStart('0'));

								ImGui.TableNextColumn();
								if (tradeItem.receiveGil > 0) ImGui.TextUnformatted(string.Format("金币: {0:0,0}", tradeItem.receiveGil).TrimStart('0'));
							}

							ImGui.EndTable();
						}
					}
					//删除记录
					if (!tradeItem.visible)
					{
						using (FileStream stream = File.Open(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create))
						{
							StreamWriter writer = new(stream);
							foreach (TradeHistory tradeHistory in tradeHistoryList)
								if (tradeHistory.visible) writer.WriteLine(tradeHistory.ToString());
							writer.Flush();
						}
						_refresh = true;
					}
				}
			}
		}

		public void PushTradeHistory(string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemArray, List<KeyValuePair<string, int>> receiveItemArray) => PushTradeHistory(true, targetName, giveGil, receiveGil, giveItemArray, receiveItemArray);

		public void PushTradeHistory(bool isSuccess, string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemList, List<KeyValuePair<string, int>> receiveItemList)
		{
			playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;


			List<TradeHistory.Item> giveList = new(), receiviList = new();
			giveItemList.ForEach(item => giveList.Add(new(item.Key, item.Value)));
			receiveItemList.ForEach(item => receiviList.Add(new(item.Key, item.Value)));

			TradeHistory tradeHistory = new()
			{
				isSuccess = isSuccess,
				targetName = targetName,
				giveGil = giveGil,
				receiveGil = receiveGil,
				giveItemArray = giveList.ToArray(),
				receiveItemArray = receiviList.ToArray()
			};

			using (FileStream stream = File.Open(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Append))
			{
				StreamWriter writer = new(stream);
				writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			_refresh = true;
		}

	}
}
