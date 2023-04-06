using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TradeRecorder.Model;

namespace TradeRecorder.Window
{
	public class History : IWindow
	{
		/// <summary>
		/// 导出交易记录表的列名
		/// </summary>
		private static readonly string[] Title = { "时间", "交易状态", "交易对象", "支付金额", "接收金额", "支付物品", "接收物品" };
		private static readonly Vector2 Img_Size = new(26, 26);
		/// <summary>
		/// 删除记录后重新去文件读取
		/// </summary>
		private bool change = false;
		private FileDialog? fileDialog;
		private List<TradeHistory> historyList = new();
		private List<TradeHistory> showList = new();

		private readonly TradeRecorder TradeRecorder;
		private bool visible = false;
		private string? target = null;

		public void Draw() {
			if (!visible) {
				if (change) {
					change = false;
					Task.Run(() => { EditHistory(); ReadHistory(); });
				}
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(480, 600), ImGuiCond.FirstUseEver);
			var title = "交易历史记录";
			if (target != null) {
				title += "-" + target;
			}
			if (ImGui.Begin(title, ref visible, ImGuiWindowFlags.NoScrollbar)) {
				if (ImGui.Button("全部清除")) { ClearHistory(); }

				ImGui.SameLine();
				ImGui.Spacing();
				ImGui.SameLine();
				if (ImGui.Button("导出到csv")) {
					fileDialog = new FileDialog("save", "导出到csv", ".csv", TradeRecorder.PluginInterface.ConfigDirectory.FullName, "output.csv", "", 1, false, ImGuiFileDialogFlags.None);
					fileDialog.Show();
				}

				if (ImGui.BeginChild("##历史清单")) {
					for (int index = 0; index < showList.Count; index++) {
						DrawHistory(index, showList[index]);
					}
				}
				// todo 增加一个角色筛选
			}

			// 判断文件保存框是否存在
			if (fileDialog != null && fileDialog.Draw()) {
				fileDialog.Hide();
				if (fileDialog.GetIsOk()) {
					var resultList = fileDialog.GetResults();
					if (resultList.Count == 1) {
						var savePath = resultList[0].Trim();
						if (!savePath.EndsWith(".csv"))
							savePath += ".csv";
						ExportHistory(savePath);
					}
				}
			}

		}

		public void ShowHistory((uint, string, string)? target = null) {
			if (!visible) {
				visible = true;
				if (target != null) {
					this.target = target?.Item2 + "@" + target?.Item3;
					showList = historyList.Where(i => i.Target == this.target).ToList();
				} else {
					this.target = null;
					showList = historyList;
				}
			} else {
				if (this.target?.Equals(this.target = target?.Item2 + "@" + target?.Item3) ?? false) {
					visible = false;
				} else {
					this.target = target?.Item2 + "@" + target?.Item3;
					showList = historyList.Where(i => i.Target == this.target).ToList();
				}
			}
		}

		/// <summary>
		/// 绘制单个交易
		/// </summary>
		/// <param name="index"></param>
		/// <param name="tradeItem"></param>
		private void DrawHistory(int index, TradeHistory tradeItem) {
			var title = $"{index + 1}:  {tradeItem.Time}  <{tradeItem.Target}>";
			if (!tradeItem.Status) { title += "  (取消)"; }
			var expansion = ImGui.CollapsingHeader(title.ToString(), ref tradeItem.visible);
			// 如果处于显示状态，则绘制本次交易的净金币进出
			if (tradeItem.visible) {
				ImGui.SameLine(ImGui.GetColumnWidth() - 130);
				var get = (int)tradeItem.ReceiveGil - (int)tradeItem.GiveGil;
				ImGui.TextUnformatted($"{(get > 0 ? "+" : "")}{get:#,#}");
			}
			if (expansion) {
				if (ImGui.BeginTable($"histroy-{index}", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg)) {
					ImGui.TableSetupColumn("支出");
					ImGui.TableSetupColumn("收入");
					ImGui.TableHeadersRow();

					// 绘制本次交易物品
					for (int i = 0; i < Math.Max(tradeItem.giveItemArray.Length, tradeItem.receiveItemArray.Length); i++) {
						ImGui.TableNextRow();
						ImGui.TableNextColumn();

						if (tradeItem.giveItemArray.Length > i) {
							var icon = tradeItem.giveItemArray[i].Icon;
							if (icon != null) {
								ImGui.Image(icon.ImGuiHandle, Img_Size);
								ImGui.SameLine();
							}
							ImGui.TextUnformatted(tradeItem.giveItemArray[i].ToShowString());
						}

						ImGui.TableNextColumn();
						if (tradeItem.receiveItemArray.Length > i) {
							var icon = tradeItem.receiveItemArray[i].Icon;
							if (icon != null) {
								ImGui.Image(icon.ImGuiHandle, Img_Size);
								ImGui.SameLine();
							}
							ImGui.TextUnformatted(tradeItem.receiveItemArray[i].ToShowString());
						}

					}

					// 如果本次交易有金币进出，则单独显示一行金币
					if (tradeItem.GiveGil > 0 || tradeItem.ReceiveGil > 0) {
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						if (tradeItem.GiveGil > 0)
							ImGui.TextUnformatted($"金币: {tradeItem.GiveGil:#,0}");

						ImGui.TableNextColumn();
						if (tradeItem.ReceiveGil > 0)
							ImGui.TextUnformatted($"金币: {tradeItem.ReceiveGil:#,0}");
					}

					ImGui.EndTable();
				}
			}
			// 删除记录
			if (!tradeItem.visible) { change = true; }
		}
		/*
		public void AddHistory(string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemArray, List<KeyValuePair<string, int>> receiveItemArray) => AddHistory(true, targetName, giveGil, receiveGil, giveItemArray, receiveItemArray);

		public void AddHistory(bool isSuccess, string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemList, List<KeyValuePair<string, int>> receiveItemList) {
			if (TradeBuddy.ClientState.LocalPlayer == null) { return; }
			var playerName = DalamudInterface.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = DalamudInterface.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			List<TradeHistory.HistoryItem> giveList = new(), receiviList = new();
			giveItemList.ForEach(i => giveList.Add(new(i.Key, i.Value)));
			receiveItemList.ForEach(i => receiviList.Add(new(i.Key, i.Value)));

			TradeHistory tradeHistory = new() {
				Status = isSuccess,
				Target = targetName,
				giveGil = giveGil,
				receiveGil = receiveGil,
				giveItemArray = giveList.ToArray(),
				receiveItemArray = receiviList.ToArray()
			};

			using (FileStream stream = File.Open(Path.Join(TradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Append)) {
				StreamWriter writer = new(stream);
				writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			Task.Run(() => ReadHistory());
		}
		*/
		public void AddHistory(bool status, string target, uint[] gil, TradeItem[][] items) {
			if (DalamudInterface.ClientState.LocalPlayer == null) {
				Chat.PrintError("历史记录追加失败，Player为空");
				return;
			}
			var playerName = DalamudInterface.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = DalamudInterface.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			var giveList = items[0].Select(i => new TradeHistory.HistoryItem(i.IconId ?? 0, i.Name ?? "<Unknown>", i.Count, i.Quality)).ToArray();
			var receiviList = items[1].Select(i => new TradeHistory.HistoryItem(i.IconId ?? 0, i.Name ?? "<Unknown>", i.Count, i.Quality)).ToArray();

			TradeHistory tradeHistory = new() {
				Status = status,
				Target = target,
				GiveGil = gil[0],
				ReceiveGil = gil[1],
				giveItemArray = giveList,
				receiveItemArray = receiviList
			};
			using (FileStream stream = File.Open(Path.Join(TradeRecorder.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Append)) {
				StreamWriter writer = new(stream);
				writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			Task.Run(() => ReadHistory());
		}

		private void ReadHistory() {
			if (TradeRecorder.ClientState.LocalPlayer == null) { return; }
			var playerName = TradeRecorder.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = TradeRecorder.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			historyList = new();

			string path = Path.Join(TradeRecorder.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

			using (StreamReader reader = new(File.Open(path, FileMode.OpenOrCreate))) {
				string tradeStr;
				while ((tradeStr = reader.ReadLine() ?? "").Length > 0) {
					TradeHistory? trade = TradeHistory.ParseFromString(tradeStr);
					if (trade != null) { historyList.Add(trade); }
				}
			}
			if (target != null) {
				showList = historyList.Where(i => i.Target == this.target).ToList();
			}
		}

		private void EditHistory() {
			if (TradeRecorder.ClientState.LocalPlayer == null) { return; }
			var playerName = TradeRecorder.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = TradeRecorder.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			using (FileStream stream = File.Open(Path.Join(TradeRecorder.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create)) {
				StreamWriter writer = new(stream);
				foreach (TradeHistory tradeHistory in historyList) {
					if (tradeHistory.visible) {
						writer.WriteLine(tradeHistory.ToString());
					}
				}
				writer.Flush();
			}
			change = false;
		}

		/// <summary>
		/// 导出当前角色所有交易记录
		/// </summary>
		/// <param name="path">保存路径</param>
		private void ExportHistory(string path) {
			if (TradeRecorder.ClientState.LocalPlayer == null)
				return;
			PluginLog.Information($"[{TradeRecorder.Name}]保存交易历史: {path}");
			var exportList = historyList.Where(i => i.visible);

			var saveList = historyList.Where(i => i.visible)
				.Select(i => new string[7] {
					i.Time,
					i.Status.ToString(),
					i.Target,
					i.GiveGil.ToString("#,0"),
					i.ReceiveGil.ToString("#,0"),
					string.Join(',', i.giveItemArray.Select(i => i.ToString())),
					string.Join(',', i.receiveItemArray.Select(i => i.ToString()))
				}).ToList();
			try {
				using (StreamWriter writer = new(File.Open(path, FileMode.Create), Encoding.UTF8)) {
					// 写入标题
					writer.WriteLine(string.Join(",", Title.Select(str => $"\"{str}\"").ToList()));

					saveList.ForEach(i => writer.WriteLine(string.Join(",", i.Select(str => $"\"{str}\"").ToList())));
					writer.Flush();
				}
			} catch (IOException e) {
				PluginLog.Error(e.ToString());
			}

		}

		/// <summary>
		/// 删除当前角色所有交易记录
		/// </summary>
		private void ClearHistory() {
			if (TradeRecorder.ClientState.LocalPlayer == null)
				return;
			var playerName = TradeRecorder.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = TradeRecorder.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			// 删除历史记录文件
			File.Delete(Path.Join(TradeRecorder.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"));

			// 清空缓存表
			historyList = new();
		}

		#region init
		public History(TradeRecorder tradeRecorder) {
			TradeRecorder = tradeRecorder;
			Task.Run(() => ReadHistory());
		}
		public void Dispose() {
			historyList.Clear();
		}
		#endregion
	}
}
