using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TradeBuddy.Window
{
	public class History : IDisposable
	{

		private static readonly string[] Title =  { "时间", "交易状态", "交易对象", "支付金额", "接收金额", "支付物品", "接收物品" };

		public class TradeHistory
		{
			public bool visible = true;//控制显示

			private const string TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
			private const char PART_SPLIT = ';';
			private const char ITEM_SPLIT = ',';
			private const char COUNT_SPLIT = 'x';
			public bool isSuccess { get; init; } = true;
			public string time { get; init; } = DateTime.Now.ToString(TIME_FORMAT);
			public string targetName { get; init; } = "";
			public int giveGil { get; init; } = 0;
			public int receiveGil { get; init; } = 0;
			public Item[] giveItemArray { get; init; } = Array.Empty<Item>();
			public Item[] receiveItemArray { get; init; } = Array.Empty<Item>();

			public class Item
			{
				public string name { get; init; }
				public int count { get; init; }
				private ushort iconId = 0;
				private bool isHQ = false;
				public TextureWrap? icon { get; init; }

				public Item(string name, int count)
				{
					this.name = name;
					this.count = count;
					isHQ = name.EndsWith("HQ");
					var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == name.Replace("HQ", String.Empty));
					if (itemByName != null) iconId = itemByName.Icon;
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
		private bool _refresh = true, _edit = false, _save = false;
		private string _path = "";
		private FileDialog? file;
		private static readonly Vector2 Img_Size = new(26, 26);
		private List<TradeHistory> _tradeHistoryList = new();
		//private string playerName = "", playerWorld = "";

		private TradeBuddy _tradeBuddy;

		public History(TradeBuddy tradeBuddy)
		{
			_tradeBuddy = tradeBuddy;
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

				ImGui.SameLine();
				ImGui.Spacing();
				ImGui.SameLine();
				if (ImGui.Button("导出到csv"))
				{
					_path = _tradeBuddy.PluginInterface.ConfigDirectory.FullName;
					file = new FileDialog("save", "导出到csv", ".csv", _path, "output.csv", "", 1, false, ImGuiFileDialogFlags.None);
					file.Show();
				}

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
									if (tradeItem.giveGil > 0) ImGui.TextUnformatted(($"金币: {tradeItem.giveGil:#,##0}"));

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

			//自带回调，但是没法设置默认路径
			/*
			var ff = new FileDialogManager();
			ff.SaveFileDialog("saveFile", "csv|*.csv", Path.Join(tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"out.csv"), "",
				(b, s) => DalamudDll.ChatGui.Print($"saveF：{b}-{s}"));
			ff.Draw();
			*/
			if (file != null && file.Draw())
			{
				_save = false;
				file.Hide();
				if (file.GetIsOk())
				{
					_path = file.GetResult();
					if (!string.IsNullOrEmpty(_path))
					{
						if (!_path.EndsWith(".csv"))
						{
							_path += ".csv";
						}
#if DEBUG
						DalamudDll.ChatGui.Print("save！Path：" + _path);
#endif
						var saveList = _tradeHistoryList.Where(i => i.visible)
							.Select(i => $"\"{i.time}\",\"{i.isSuccess}\",\"{i.targetName}\",\"{i.giveGil:#,##0}\",\"{i.receiveGil:#,##0}\",\"{string.Join(',', i.giveItemArray.Select(i => i.ToString()))}\",\"{string.Join(',', i.receiveItemArray.Select(i => i.ToString()))}\"").ToList();
						try
						{
							using (StreamWriter writer = new(File.Open(_path, FileMode.Create), Encoding.UTF8))
							{
								writer.WriteLine(string.Join(',', Title.Select(i => $"\"{i}\"").ToList()));
								saveList.ForEach(i => writer.WriteLine(i));
								writer.Flush();
							}
						}
						catch (IOException e)
						{
							PluginLog.Error(e.ToString());
						}
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

			using (FileStream stream = File.Open(Path.Join(_tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Append))
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

			string path = Path.Join(_tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

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

			using (FileStream stream = File.Open(Path.Join(_tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create))
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

			File.Delete(Path.Join(_tradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"));
		}

		public void Dispose()
		{
			_tradeHistoryList.Clear();
		}
	}
}
