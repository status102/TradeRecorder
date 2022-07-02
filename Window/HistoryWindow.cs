using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace TradeBuddy.Window
{
	public class HistoryWindow
	{
		private bool refresh = true;
		private List<TradeHistory> tradeHistoryList = new List<TradeHistory>();
		private string playerName = "", playerWorld = "";

		public class TradeHistory
		{
			public bool visible = true;//控制显示

			private readonly static string TimeFormatStr = "yyyy-MM-dd HH:mm:ss";
			private readonly static char partSplitChar = ';';
			private readonly static char itemSplitChar = ',';
			public bool isSuccess = true;
			public string time = DateTime.Now.ToString(TimeFormatStr);
			public string targetName = "";
			public int giveGil = 0, receiveGil = 0;
			public Item[] giveItemArray = Array.Empty<Item>(), receiveItemArray = Array.Empty<Item>();

			public class Item
			{
				public string name { get; set; }
				public int count { get; set; }

				public Item(string name, int count)
				{
					this.name = name;
					this.count = count;
				}
				public override string ToString() => name + "x" + count;
				public string ToLongString() => name + " x " + count;
			}
			public static TradeHistory? ParseFromString(string str)
			{
				string[] strArray = str.Split(partSplitChar);
				TradeHistory tradeHistory;
				string[] giveItemStr, receiveItemStr;
				if (strArray.Length != 7) return null;

				tradeHistory = new TradeHistory()
				{
					time = strArray[0],
					isSuccess = Convert.ToBoolean(strArray[1]),
					targetName = strArray[2],
					giveGil = Convert.ToInt32(strArray[3]),
					receiveGil = Convert.ToInt32(strArray[4])
				};

				giveItemStr = strArray[5].Split(itemSplitChar);
				receiveItemStr = strArray[6].Split(itemSplitChar);
				
				List<Item> giveList = new(), receiveList = new();

				for (int i = 0; i < giveItemStr.Length; i++)
				{
					string[] itemStr = giveItemStr[i].Split('x');
					if (itemStr.Length == 2)
					{
						giveList.Add(new Item(itemStr[0], Convert.ToInt32(itemStr[1])));
					}
				}

				for (int i = 0; i < receiveItemStr.Length; i++)
				{
					string[] itemStr = receiveItemStr[i].Split('x');
					if (itemStr.Length == 2)
					{
						receiveList.Add(new Item(itemStr[0], Convert.ToInt32(itemStr[1])));
					}
				}
				tradeHistory.giveItemArray = giveList.ToArray();
				tradeHistory.receiveItemArray = receiveList.ToArray();

				return tradeHistory;
			}
			public override string ToString()
			{
				string[] write = new string[7];
				write[0] = time;
				write[1] = Convert.ToString(isSuccess);
				write[2] = targetName;
				write[3] = Convert.ToString(giveGil);
				write[4] = Convert.ToString(receiveGil);
				write[5] = String.Join<Item>(itemSplitChar, giveItemArray);
				write[6] = String.Join<Item>(itemSplitChar, receiveItemArray);
				return String.Join(partSplitChar, write);
			}
		}

		public void DrawHistory(ref bool historyVisible)
		{
			if (!historyVisible) return;

			playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			if (refresh)
			{
				tradeHistoryList.Clear();

				//tode 读取记录
				string tradeStr, path = Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

				using (FileStream stream = File.Open(path, FileMode.OpenOrCreate))
				{
					StreamReader reader = new StreamReader(stream);
					while ((tradeStr = reader.ReadLine() ?? "").Length > 0)
					{
						if (tradeStr.Split(';').Length > 1)
						{
							TradeHistory? trade = TradeHistory.ParseFromString(tradeStr);
							if (trade != null && trade.time.Length > 0) tradeHistoryList.Add(trade);
						}
					}
				}
				refresh = false;
			}
			ImGui.SetNextWindowSize(new Vector2(480, 600), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("交易历史记录", ref historyVisible))
			{
				if (ImGui.Button("全部清除"))
				{
					using (FileStream stream = File.Open(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create))
					{
						StreamWriter writer = new(stream);
						writer.Flush();
						refresh = true;
					}
				}

				for (int index = 0; index < tradeHistoryList.Count; index++)
				{
					TradeHistory tradeItem = tradeHistoryList[index];
					if (tradeItem == null || tradeItem.time.Length == 0) continue;
					StringBuilder title = new();
					title = title.Append(index + 1).Append(":  ").Append(tradeItem.time).Append("  <").Append(tradeItem.targetName).Append('>');
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
									ImGui.TextUnformatted(tradeItem.giveItemArray[i].ToLongString());
								//else
									//ImGui.TextUnformatted("");

								ImGui.TableNextColumn();
								if (tradeItem.receiveItemArray.Length > i)
									ImGui.Text(tradeItem.receiveItemArray[i].ToLongString());
								//else
									//ImGui.TextUnformatted("");
							}
							if (tradeItem.giveGil > 0 || tradeItem.receiveGil > 0)
							{
								ImGui.TableNextRow();
								ImGui.TableNextColumn();
								if (tradeItem.giveGil > 0) ImGui.Text(String.Format("金币: {0:0,0}", tradeItem.giveGil).TrimStart('0'));

								ImGui.TableNextColumn();
								if (tradeItem.receiveGil > 0) ImGui.Text(String.Format("金币: {0:0,0}", tradeItem.receiveGil).TrimStart('0'));
							}

							ImGui.EndTable();
						}
					}
					//删除记录
					if (!tradeItem.visible)
					{
						using (FileStream stream = File.Open(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create))
						{
							StreamWriter writer = new StreamWriter(stream);
							foreach (TradeHistory tradeHistory in tradeHistoryList)
								if (tradeHistory.visible) writer.WriteLine(tradeHistory.ToString());
							writer.Flush();
						}
						refresh = true;
					}
				}
			}
		}

		public void PushTradeHistory(string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemArray, List<KeyValuePair<string, int>> receiveItemArray) => PushTradeHistory(true, targetName, giveGil, receiveGil, giveItemArray, receiveItemArray);

		public void PushTradeHistory(bool isSuccess, string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemList, List<KeyValuePair<string, int>> receiveItemList)
		{
			playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			TradeHistory tradeHistory = new()
			{
				isSuccess = isSuccess,
				targetName = targetName,
				giveGil = giveGil,
				receiveGil = receiveGil
			};

			tradeHistory.giveItemArray = new TradeHistory.Item[giveItemList.Count];
			tradeHistory.receiveItemArray = new TradeHistory.Item[receiveItemList.Count];
			List<TradeHistory.Item> list = new();
			giveItemList.ForEach(item => list.Add(new TradeHistory.Item(item.Key, item.Value)));
			tradeHistory.giveItemArray = list.ToArray();

			list = new();
			receiveItemList.ForEach(item => list.Add(new TradeHistory.Item(item.Key, item.Value)));
			tradeHistory.receiveItemArray = list.ToArray();

			using (FileStream stream = File.Open(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Append))
			{
				StreamWriter writer = new(stream);
				writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			refresh = true;
		}

	}
}
