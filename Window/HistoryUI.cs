using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace TradeBuddy.Window
{
	public class HistoryUI
	{
		private static bool refresh = true;
		private static List<TradeHistory> tradeHistoryList = new List<TradeHistory>();
		public class TradeHistory
		{
			public static string TimeFormatStr = "yyyy-MM-dd HH:mm:ss";
			private static char partSplitChar = ';';
			public string time;
			public string targetName;
			public int giveGil, receiveGil;
			public Item[] giveItemArray = Array.Empty<Item>(), receiveItemArray = Array.Empty<Item>();

			public TradeHistory(string time, string targetName)
			{
				this.time = time;
				this.targetName = targetName;
			}

			public class Item
			{
				public string name { get; set; }
				public int count { get; set; }
			}
			public static TradeHistory? parseFromString(string str)
			{
				string[] strArray = str.Split(partSplitChar);
				if (strArray.Length != 6) return null;
				TradeHistory tradeHistory = new TradeHistory(strArray[0], strArray[1]);
				tradeHistory.giveGil = Convert.ToInt32(strArray[2]);
				tradeHistory.receiveGil = Convert.ToInt32(strArray[4]);

				string[] giveItemStr = strArray[3].Split(',');
				string[] receiveItemStr = strArray[5].Split(',');

				tradeHistory.giveItemArray = new Item[giveItemStr.Length];
				tradeHistory.receiveItemArray = new Item[receiveItemStr.Length];

				string[] itemStr;
				for (int i = 0; i < giveItemStr.Length; i++)
				{
					itemStr = giveItemStr[i].Split('x');
					if (itemStr.Length == 2)
					{
						tradeHistory.giveItemArray[i].name = itemStr[0];
						tradeHistory.giveItemArray[i].count = Convert.ToInt32(itemStr[1]);
					}
					else
					{
						tradeHistory.giveItemArray[i].name = "(获取失败)";
						tradeHistory.giveItemArray[i].count = 0;
					}
				}

				for (int i = 0; i < receiveItemStr.Length; i++)
				{
					itemStr = receiveItemStr[i].Split('x');
					if (itemStr.Length == 2)
					{
						tradeHistory.receiveItemArray[i].name = itemStr[0];
						tradeHistory.receiveItemArray[i].count = Convert.ToInt32(itemStr[1]);
					}
					else
					{
						tradeHistory.receiveItemArray[i].name = "(获取失败)";
						tradeHistory.receiveItemArray[i].count = 0;
					}
				}

				return tradeHistory;
			}
			public override string ToString()
			{
				string[] write = new string[5];
				write[0] = targetName;
				write[1] = Convert.ToString(giveGil);
				write[2] = "";
				foreach (Item item in giveItemArray)
					if (item.count > 0) write[2] += item.name + "x" + item.count + ',';
				write[2] = write[2].TrimEnd(',');
				write[3] = Convert.ToString(receiveGil);
				write[4] = "";
				foreach (Item item in giveItemArray)
					if (item.count > 0) write[4] += item.name + "x" + item.count + ',';
				write[4] = write[4].TrimEnd(',');

				StringBuilder output = new StringBuilder(time);
				foreach (string item in write)
					output = output.Append(partSplitChar).Append(item);
				return output.ToString();
			}
		}

		public static void DrawHistory(ref bool historyVisible)
		{
			if (!historyVisible) return;
			if (refresh)
			{
				string playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
				string playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

				//tode 读取记录
				string tradeStr;
				using (StreamReader reader = new StreamReader(File.OpenRead(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"))))
				{
					tradeStr = reader.ReadLine() ?? "";
					if (tradeStr.Split(';').Length > 0)
					{
						TradeHistory? trade = TradeHistory.parseFromString(tradeStr);
						if (trade != null && trade.time.Length > 0) tradeHistoryList.Add(trade);
					}
				}
				refresh = false;
			}
			ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("历史记录", ref historyVisible))
			{
				foreach (TradeHistory trade in tradeHistoryList)
				{
					if (trade.time.Length == 0) continue;
					StringBuilder title = new StringBuilder(trade.time);
					title = title.Append(": ").Append(trade.targetName);
					if (trade.giveGil > 0) title = title.Append("  <--").Append(trade.giveGil);
					if (trade.receiveGil > 0) title = title.Append("  -->").Append(trade.receiveGil);
					if (ImGui.TreeNode(title.ToString()))
					{
						//ImGui.Indent();
						if (ImGui.BeginTable("histroy", 2))
						{
							for (int i = 0; i < 5; i++)
							{
								ImGui.TableNextColumn();

								ImGui.Bullet();
								ImGui.SameLine();
								if (trade.giveItemArray.Length <= i && trade.receiveItemArray.Length <= i) break;
								if (trade.giveItemArray.Length > i) ImGui.Text(trade.giveItemArray[i].name + " x " + trade.giveItemArray[i].count);
								else ImGui.Text("");

								ImGui.SameLine();
								if (trade.receiveItemArray.Length > i) ImGui.Text(trade.receiveItemArray[i].name + " x " + trade.receiveItemArray[i].count);
								else ImGui.Text("");
							}
							ImGui.EndTable();
						}
						//ImGui.Unindent();
					}
				}
			}
		}

		public static void PushTradeHistory(string targetName, int giveGil, TradeUI.Item[] giveItemArray, int receiveGil, TradeUI.Item[] receiveItemArray)
		{
			string playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			string playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			TradeHistory tradeHistory = new TradeHistory(DateTime.Now.ToString(TradeHistory.TimeFormatStr), targetName)
			{
				giveGil = giveGil,
				receiveGil = receiveGil
			};

			int giveItemCount = 0, receiveItemCount = 0;
			foreach (TradeUI.Item item in giveItemArray) if (item.count > 0) giveItemCount++;
			foreach (TradeUI.Item item in receiveItemArray) if (item.count > 0) receiveItemCount++;

			tradeHistory.giveItemArray = new TradeHistory.Item[giveItemCount];
			tradeHistory.receiveItemArray = new TradeHistory.Item[receiveItemCount];

			giveItemCount = 0;
			receiveItemCount = 0;

			foreach (TradeUI.Item item in giveItemArray) if (item.count > 0)
				{
					tradeHistory.giveItemArray[giveItemCount].name = item.name;
					tradeHistory.giveItemArray[giveItemCount].count = item.count;
					giveItemCount++;
				}
			foreach (TradeUI.Item item in receiveItemArray) if (item.count > 0)
				{
					tradeHistory.receiveItemArray[receiveItemCount].name = item.name;
					tradeHistory.receiveItemArray[receiveItemCount].count = item.count;
					receiveItemCount++;
				}

			tradeHistoryList.Add(tradeHistory);
			using (StreamWriter writer = File.AppendText(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt")))
				writer.WriteLine(tradeHistory.ToString());
		}
	}
}
