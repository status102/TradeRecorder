﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace TradeBuddy.Window
{
	public class History
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

				List<Item> giveList = new List<Item>(), receiveList = new List<Item>();

				for (int i = 0; i < giveItemStr.Length; i++)
				{
					string[] itemStr = giveItemStr[i].Split('x');
					if (itemStr.Length == 2)
					{
						Item item = new Item()
						{
							name = itemStr[0],
							count = Convert.ToInt32(itemStr[1])
						};
						giveList.Add(item);
					}
				}
				tradeHistory.giveItemArray = giveList.ToArray();

				for (int i = 0; i < receiveItemStr.Length; i++)
				{
					string[] itemStr = receiveItemStr[i].Split('x');
					if (itemStr.Length == 2)
					{
						Item item = new Item()
						{
							name = itemStr[0],
							count = Convert.ToInt32(itemStr[1])
						};
						receiveList.Add(item);
					}
				}
				tradeHistory.receiveItemArray = receiveList.ToArray();

				return tradeHistory;
			}
			public override string ToString()
			{
				string[] write = new string[5];
				write[0] = targetName;
				write[1] = Convert.ToString(giveGil);
				write[2] = "";

				foreach (Item item in giveItemArray) write[2] += item.name + "x" + item.count + ',';

				write[2] = write[2].TrimEnd(',');
				write[3] = Convert.ToString(receiveGil);
				write[4] = "";

				foreach (Item item in receiveItemArray) write[4] += item.name + "x" + item.count + ',';
				write[4] = write[4].TrimEnd(',');

				StringBuilder output = new StringBuilder(time);
				foreach (string item in write) output = output.Append(partSplitChar).Append(item);
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
				string tradeStr, path = Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

				using (FileStream stream = File.Open(path, FileMode.OpenOrCreate))
				{
					StreamReader reader = new StreamReader(stream);
					while ((tradeStr = reader.ReadLine() ?? "").Length > 0)
					{
						if (tradeStr.Split(';').Length > 1)
						{
							TradeHistory? trade = TradeHistory.parseFromString(tradeStr);
							if (trade != null && trade.time.Length > 0) tradeHistoryList.Add(trade);
						}
					}
				}
				refresh = false;
			}
			ImGui.SetNextWindowSize(new Vector2(320, 300), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("历史记录", ref historyVisible))
			{
				foreach (TradeHistory trade in tradeHistoryList)
				{
					if (trade.time.Length == 0) continue;
					StringBuilder title = new StringBuilder(trade.time);
					title = title.Append(": ").Append(trade.targetName);
					if (trade.giveGil > 0) title = title.Append("  <--").Append(trade.giveGil);
					if (trade.receiveGil > 0) title = title.Append(' ', 40).Append("  -->").Append(trade.receiveGil);
					if (ImGui.CollapsingHeader(title.ToString()))
					{
						if (ImGui.BeginTable("histroy", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
						{
							for (int i = 0; i < 5; i++)
							{
								if (trade.giveItemArray.Length <= i && trade.receiveItemArray.Length <= i) break;

								ImGui.TableNextColumn();

								ImGui.Bullet();
								ImGui.SameLine();

								if (trade.giveItemArray.Length > i) ImGui.Text(trade.giveItemArray[i].name + " x " + trade.giveItemArray[i].count);
								else ImGui.Text("");

								ImGui.TableNextColumn();
								if (trade.receiveItemArray.Length > i) ImGui.Text(trade.receiveItemArray[i].name + " x " + trade.receiveItemArray[i].count);
								else ImGui.Text("");
							}
							ImGui.EndTable();
						}
					}
				}
			}
		}

		public static void PushTradeHistory(string targetName, int giveGil, Trade.Item[] giveItemArray, int receiveGil, Trade.Item[] receiveItemArray)
		{
			string playerName = DalamudDll.ClientState.LocalPlayer!.Name.TextValue;
			string playerWorld = DalamudDll.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			TradeHistory tradeHistory = new TradeHistory(DateTime.Now.ToString(TradeHistory.TimeFormatStr), targetName)
			{
				giveGil = giveGil,
				receiveGil = receiveGil
			};

			List<TradeHistory.Item> giveItemList = new List<TradeHistory.Item>(), receiveItemList = new List<TradeHistory.Item>();
			for (int i = 0; i < giveItemArray.Length; i++)
			{
				if (giveItemArray[i] != null && giveItemArray[i].count > 0)
					giveItemList.Add(new TradeHistory.Item()
					{
						name = giveItemArray[i].name,
						count = giveItemArray[i].count
					});
			}
			for (int i = 0; i < receiveItemArray.Length; i++)
			{
				if (receiveItemArray[i] != null && receiveItemArray[i].count > 0)
					receiveItemList.Add(new TradeHistory.Item()
					{
						name = receiveItemArray[i].name,
						count = receiveItemArray[i].count
					});
			}

			tradeHistory.giveItemArray = giveItemList.ToArray();
			tradeHistory.receiveItemArray = receiveItemList.ToArray();
						
			string path = Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");
			using (FileStream stream = File.Open(path, FileMode.OpenOrCreate))
			{
				stream.Position = stream.Length;
				StreamWriter writer = new StreamWriter(stream);
				writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			refresh = true;
		}
	}
}