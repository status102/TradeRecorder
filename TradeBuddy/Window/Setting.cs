using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TradeBuddy.Model;

namespace TradeBuddy.Window
{
	public class Setting : IExternalWindow
	{
		private readonly static Vector2 Window_Size = new(720, 640);
		private readonly static Vector2 Image_Size = new(54, 54);
		private readonly static Vector4 Alert_Color = new(208 / 255f, 177 / 255f, 50 / 255f, 1);
		private const int Item_width = 190, Item_Interval = 5;

		private bool firstDraw = false;
		private int editIndex = -1, moreEditIndex = -1;
		private string nameLabel = "", priceLabel = "";
		private readonly List<PresetItem> itemList;
		private  TextureWrap? failureImage => TradeBuddy.GetIcon(784);
		private readonly TradeBuddy TradeBuddy;
		private Configuration Config => TradeBuddy.Configuration;
		public Setting(TradeBuddy tradeBuddy) {
			this.TradeBuddy = tradeBuddy;
			itemList = tradeBuddy.Configuration.PresetItemList;
		}
		public void Draw(ref bool _settingVisible) {
			if (!_settingVisible) {
				firstDraw = true;
				editIndex = -1;
				return;
			}

			//窗口启动时检查未获取价格的道具
			if (firstDraw) {
				itemList.ForEach(item => { if (item.minPrice == -2) item.UpdateMinPrice(); });
				firstDraw = false;
			}
			ImGui.SetNextWindowSize(Window_Size, ImGuiCond.FirstUseEver);
			if (ImGui.Begin(TradeBuddy.Name + "插件设置", ref _settingVisible)) {
				if (ImGui.CollapsingHeader("基础设置", ImGuiTreeNodeFlags.DefaultOpen)) {
					ImGui.Indent();
					if (ImGui.Checkbox("显示监控窗口", ref Config.ShowTrade))
						Config.Save();

					ImGui.SetNextItemWidth(300);
					if (ImGui.InputText("##确认交易字符串", ref Config.TradeConfirmStr, 256))
						Config.Save();

					ImGui.SameLine();
					if (ImGui.Checkbox("确认交易后提示", ref Config.TradeConfirmAlert))
						Config.Save();

					ImGui.SetNextItemWidth(300);
					if (ImGui.InputText("##取消交易字符串", ref Config.TradeCancelStr, 256))
						Config.Save();

					ImGui.SameLine();
					if (ImGui.Checkbox("取消交易后提示", ref Config.TradeCancelAlert))
						Config.Save();

					if (ImGui.Checkbox("##绘制不低于预期", ref Config.DrawRetainerSellListProper))
						Config.Save();

					ImGui.SameLine();
					ImGui.SetNextItemWidth(300);
					var properColor = Config.SellList.ProperColor;
					if (ImGui.ColorEdit3("雇员出售价格不低于预期", ref properColor)) {
						Config.SellList.ProperColor = properColor;
						Config.Save();
					}
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(0, FontAwesomeIcon.Reply)) {
						Array.Copy(Configuration.RetainerSellList.Proper_Color_Default, Config.SellList.ProperColorArray, 3);
						Config.Save();
					}
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("重置");

					if (ImGui.Checkbox("##绘制低于预期", ref Config.DrawRetainerSellListAlert))
						Config.Save();
					ImGui.SameLine();

					var alertColor = Config.SellList.AlertColor;
					ImGui.SetNextItemWidth(300);
					if (ImGui.ColorEdit3("雇员出售价格低于预期", ref alertColor)) {
						Config.SellList.AlertColor = alertColor;
						Config.Save();
					}
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(1, FontAwesomeIcon.Reply)) {
						Array.Copy(Configuration.RetainerSellList.Alert_Color_Default, Config.SellList.AlertColorArray, 3);
						Config.Save();
					}
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("重置");

					ImGui.Unindent();
				}

				if (ImGui.CollapsingHeader("预期价格")) {

					#region 编辑块
					//添加预期的按钮
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Plus)) {
						editIndex = -2;

						string clipboard = "";
						try {
							clipboard = ImGui.GetClipboardText().Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim().Replace("", "HQ");
						} catch (NullReferenceException) { }
						nameLabel = "";
						priceLabel = "0";

						foreach (char c in clipboard) {
							if (c >= '0' && c <= '9')
								priceLabel += c;
							else
								nameLabel += c;
						}
						priceLabel = priceLabel.TrimStart('0');
						priceLabel = string.IsNullOrEmpty(priceLabel) ? "0" : priceLabel;
					}

					//删除所有预期
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Trash)) {
						itemList.Clear();
						Config.Save();
					}
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("删除所有项目");

					//手动刷新价格
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Sync))
						itemList.ForEach(item => item.UpdateMinPrice());
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("重新获取所有价格(数据来自Universalis)");

					//导出到剪贴板
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Upload))
						ImGui.SetClipboardText(string.Join('\n', itemList.Select(i => i.ToString())));
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("导出到剪贴板");

					//从剪贴板导入
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Download)) {
						try {
							string clipboard = ImGui.GetClipboardText().Trim();
							string[] strLine = clipboard.Split('\n');
							foreach (string line in strLine) {
								if (!string.IsNullOrEmpty(line))
									itemList.Add(PresetItem.ParseFromString(line));
							}
						} catch (NullReferenceException e) {
							PluginLog.Error("从剪贴板导入失败\n" + e.ToString());
						}
						Config.Save();
					}
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("从剪贴板导入");

					ImGui.SameLine();
					ImGui.TextDisabled("(?)");
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("双击鼠标左键物品编辑物品名，单击鼠标右键打开详细编辑\n不同价格方案间以;分割\n当最低价低于设定价格时，标黄进行提醒");

					//添加or编辑预期中
					if (editIndex != -1 && editIndex < itemList.Count) {
						bool save = false;
						//保存设置
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Check) && !string.IsNullOrEmpty(nameLabel))
							save = true;

						//取消编辑
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Times))
							editIndex = -1;

						ImGui.InputText("名字", ref nameLabel, 256, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13])
							save = true;

						ImGui.InputText("价格", ref priceLabel, 1022564, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13])
							save = true;

						int current_index = -1;
						string[] items = SearchName(nameLabel).ToArray();
						if (ImGui.ListBox("##候选表", ref current_index, items, items.Length, 3))
							nameLabel = items[current_index];

						if (save && Config.PresetItemDictionary.ContainsKey(nameLabel) && editIndex != Config.PresetItemDictionary[nameLabel]) {
							TradeBuddy.ChatGui.PrintError("物品与已有设定重复，无法添加");
							save = false;
						} else if (save) {
							if (nameLabel == "") {
								itemList.RemoveAt(editIndex);
							} else {
								if (editIndex == -2) {
									PresetItem presetItem = new();
									itemList.Add(presetItem);

									editIndex = itemList.Count - 1;
								}
								itemList[editIndex].ItemName = nameLabel;
								itemList[editIndex].SetPriceStr(priceLabel.Replace("-", string.Empty).Replace(",", string.Empty));
							}
							Config.Save();
							editIndex = -1;
						}
					}
					#endregion
					int rowIndex = 0;
					for (int i = 0; i < itemList.Count; i++) {
						if (ImGui.GetColumnWidth() < (Item_width + Item_Interval) * (rowIndex + 1) + 8)
							rowIndex = 0;

						if (rowIndex > 0)
							ImGui.SameLine(rowIndex * (Item_width + Item_Interval) + 8);
						rowIndex++;

						DrawItemBlock(i, itemList[i]);
					}
				}
				ImGui.End();
			}
		}
		/// <summary>
		/// 绘制单个道具的方块
		/// </summary>
		/// <param name="id">通过不重复的id来区别不同东西</param>
		/// <param name="item"></param>
		private void DrawItemBlock(int index, PresetItem item) {
			if (ImGui.BeginChild($"##ItemBlock-{index}", new(Item_width, Image_Size.Y + 16), true)) {
				if (item.iconId > 0) {
					TextureWrap? texture = TradeBuddy.GetIcon(item.iconId, item.isHQ);
					if (texture != null)
						ImGui.Image(texture.ImGuiHandle, Image_Size);
					ImGui.SameLine();
				} else {
					if (failureImage != null)
						ImGui.Image(failureImage.ImGuiHandle, Image_Size);
					ImGui.SameLine();
				}

				ImGui.BeginGroup();

				//市场出售价更低时显示黄色进行提醒;
				ImGui.TextUnformatted(item.minPriceServer);
				if (item.minPrice < 1)
					ImGui.TextUnformatted(item.GetMinPriceStr(false));
				else if (item.minPrice < item.EvaluatePrice())
					ImGui.TextColored(Alert_Color, $"{item.minPrice:#,0}");
				else
					ImGui.TextUnformatted($"{item.minPrice:#,0}");

				ImGui.EndGroup();

				ImGui.EndChild();
			}
			
			if (ImGui.IsItemHovered()) {
				ImGui.BeginTooltip();
				ImGui.TextUnformatted($"物品名: {item.ItemName}");
				ImGui.TextUnformatted($"预设价格(价格/个): {item.GetPriceStr()}");
				if (item.minPrice > 0) {
					ImGui.TextUnformatted($"大区最低: {item.GetMinPriceStr(true)}");
					ImGui.TextUnformatted($"更新时间: {item.GetMinPriceUpdateTimeStr()}");
				}
				ImGui.EndTooltip();

				if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
					editIndex = index;
					nameLabel = item.ItemName;
					priceLabel = item.GetPriceStr();
				}
				if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
					ImGui.OpenPopup($"MoreEdit-{index}");
					moreEditIndex = index;
				}
			}
			if (ImGui.BeginPopup($"MoreEdit-{index}", ImGuiWindowFlags.NoMove)) {
				// 编辑当前物品
				if (ImGuiComponents.IconButton(-moreEditIndex - 1, FontAwesomeIcon.Edit)) {
					editIndex = moreEditIndex;
					nameLabel = itemList[moreEditIndex].ItemName;
					priceLabel = itemList[moreEditIndex].GetPriceStr();
					ImGui.CloseCurrentPopup();
				}
				// 重新获取当前物品的最低价格
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(-moreEditIndex - 2, FontAwesomeIcon.Sync)) {
					itemList[moreEditIndex].UpdateMinPrice();
					ImGui.CloseCurrentPopup();
				}
				// 删除当前物品
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(-moreEditIndex - 3, FontAwesomeIcon.Trash)) {
					itemList.RemoveAt(moreEditIndex);
					Config.Save();
					editIndex = -1;
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
		}

		/// <summary>
		/// 搜索含有关键词的道具名称
		/// </summary>
		private List<string> SearchName(string name, bool onlyTradable = false) {
			var resultList = new List<string>();
			if (!string.IsNullOrEmpty(name)) {
				List<string>? tradeList;
				if (onlyTradable) {
					tradeList = TradeBuddy.DataManager.GetExcelSheet<Item>()?.Where(i => i.Name.ToString().Contains(name) && !i.IsUntradable).Select(i => i.Name.RawString).ToList();
				} else {
					tradeList = TradeBuddy.DataManager.GetExcelSheet<Item>()?.Where(i => i.Name.ToString().Contains(name)).Select(i => i.Name.RawString).ToList();
				}
				tradeList?.Where(i => i.StartsWith(name)).ToList().ForEach(i => resultList.Add(i));
				tradeList?.Where(i => !i.StartsWith(name)).ToList().ForEach(i => resultList.Add(i));

			}
			return resultList;
		}
		public void Dispose() { }
	}
}
