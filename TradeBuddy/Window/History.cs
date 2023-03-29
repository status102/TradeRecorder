﻿using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TradeBuddy.Model;

namespace TradeBuddy.Window
{
	public class History : IExternalWindow
	{
		/// <summary>
		/// 导出交易记录表的列名
		/// </summary>
		private static readonly string[] Title = { "时间", "交易状态", "交易对象", "支付金额", "接收金额", "支付物品", "接收物品" };
		private static readonly Vector2 Img_Size = new(26, 26);
		/// <summary>
		/// 删除记录后重新去文件读取
		/// </summary>
		private bool _change = false;
		private FileDialog? fileDialog;
		private List<TradeHistory> tradeHistoryList = new();

		private readonly TradeBuddy TradeBuddy;

		public void Draw(ref bool historyVisible) {
			if (!historyVisible) {
				if (_change) {
					_change = false;
					Task.Run(() => { EditHistory(); ReadHistory(); });
				}
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(480, 600), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("交易历史记录", ref historyVisible, ImGuiWindowFlags.NoScrollbar)) {
				if (ImGui.Button("全部清除"))
					ClearHistory();

				ImGui.SameLine();
				ImGui.Spacing();
				ImGui.SameLine();
				if (ImGui.Button("导出到csv")) {
					fileDialog = new FileDialog("save", "导出到csv", ".csv", TradeBuddy.PluginInterface.ConfigDirectory.FullName, "output.csv", "", 1, false, ImGuiFileDialogFlags.None);
					fileDialog.Show();
				}

				if (ImGui.BeginChild("##历史清单")) {
					for (int index = 0; index < tradeHistoryList.Count; index++) {
						DrawHistory(index, tradeHistoryList[index]);
					}
				}
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
						SaveHistory(savePath);
					}
				}
			}

		}

		// 绘制单个交易
		public void DrawHistory(int index, TradeHistory tradeItem) {
			var title = $"{index + 1}:  {tradeItem.time}  <{tradeItem.targetName}>";
			if (!tradeItem.isSuccess)
				title += "  (取消)";
			var expansion = ImGui.CollapsingHeader(title.ToString(), ref tradeItem.visible);
			// 如果处于显示状态，则绘制本次交易的净金币进出
			if (tradeItem.visible) {
				ImGui.SameLine(ImGui.GetColumnWidth() - 130);
				var get = tradeItem.receiveGil - tradeItem.giveGil;
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
							var icon = tradeItem.giveItemArray[i].icon;
							if (icon != null) {
								ImGui.Image(icon.ImGuiHandle, Img_Size);
								ImGui.SameLine();
							}
							ImGui.TextUnformatted(tradeItem.giveItemArray[i].ToShowString());
						}

						ImGui.TableNextColumn();
						if (tradeItem.receiveItemArray.Length > i) {
							var icon = tradeItem.receiveItemArray[i].icon;
							if (icon != null) {
								ImGui.Image(icon.ImGuiHandle, Img_Size);
								ImGui.SameLine();
							}
							ImGui.TextUnformatted(tradeItem.receiveItemArray[i].ToShowString());
						}

					}
					
					// 如果本次交易有金币进出，则单独显示一行金币
					if (tradeItem.giveGil > 0 || tradeItem.receiveGil > 0) {
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						if (tradeItem.giveGil > 0)
							ImGui.TextUnformatted($"金币: {tradeItem.giveGil:#,0}");

						ImGui.TableNextColumn();
						if (tradeItem.receiveGil > 0)
							ImGui.TextUnformatted($"金币: {tradeItem.receiveGil:#,0}");
					}

					ImGui.EndTable();
				}
			}
			// 删除记录
			if (!tradeItem.visible)
				_change = true;

		}

		public void AddHistory(string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemArray, List<KeyValuePair<string, int>> receiveItemArray) => AddHistory(true, targetName, giveGil, receiveGil, giveItemArray, receiveItemArray);

		public void AddHistory(bool isSuccess, string targetName, int giveGil, int receiveGil, List<KeyValuePair<string, int>> giveItemList, List<KeyValuePair<string, int>> receiveItemList) {
			if (TradeBuddy.ClientState.LocalPlayer == null)
				return;
			var playerName = TradeBuddy.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = TradeBuddy.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			List<TradeHistory.HistoryItem> giveList = new(), receiviList = new();
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

			using (FileStream stream = File.Open(Path.Join(TradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Append)) {
				StreamWriter writer = new(stream);
				writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			Task.Run(() => ReadHistory());
		}

		public void ReadHistory() {
			if (TradeBuddy.ClientState.LocalPlayer == null)
				return;
			var playerName = TradeBuddy.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = TradeBuddy.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			tradeHistoryList = new();

			string path = Path.Join(TradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

			using (StreamReader reader = new(File.Open(path, FileMode.OpenOrCreate))) {
				string tradeStr;
				while ((tradeStr = reader.ReadLine() ?? "").Length > 0) {
					TradeHistory? trade = TradeHistory.ParseFromString(tradeStr);
					if (trade != null)
						tradeHistoryList.Add(trade);
				}
			}
		}

		public void EditHistory() {
			if (TradeBuddy.ClientState.LocalPlayer == null)
				return;
			var playerName = TradeBuddy.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = TradeBuddy.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			using (FileStream stream = File.Open(Path.Join(TradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create)) {
				StreamWriter writer = new(stream);
				foreach (TradeHistory tradeHistory in tradeHistoryList)
					if (tradeHistory.visible)
						writer.WriteLine(tradeHistory.ToString());
				writer.Flush();
			}
			_change = false;
		}

		/// <summary>
		/// 导出当前角色所有交易记录
		/// </summary>
		/// <param name="path">保存路径</param>
		public void SaveHistory(string path) {
			if (TradeBuddy.ClientState.LocalPlayer == null)
				return;
			PluginLog.Information($"[{TradeBuddy.Name}]保存交易历史: {path}");
			var saveList = tradeHistoryList.Where(i => i.visible)
				.Select(i => new string[7] { i.time, i.isSuccess.ToString(), i.targetName, i.giveGil.ToString("#,0"), i.receiveGil.ToString("#,0"), string.Join(',', i.giveItemArray.Select(i => i.ToString())), string.Join(',', i.receiveItemArray.Select(i => i.ToString())) })
				.ToList();
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
		public void ClearHistory() {
			if (TradeBuddy.ClientState.LocalPlayer == null)
				return;
			var playerName = TradeBuddy.ClientState.LocalPlayer!.Name.TextValue;
			var playerWorld = TradeBuddy.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

			File.Delete(Path.Join(TradeBuddy.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"));
		}

		#region init
		public History(TradeBuddy tradeBuddy) {
			TradeBuddy = tradeBuddy;
			Task.Run(() => ReadHistory());
		}
		public void Dispose() {
			tradeHistoryList.Clear();
		}
		#endregion
	}
}