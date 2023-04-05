using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using TradeRecorder.Model;

namespace TradeRecorder.Window
{
	public class Setting : IWindow
	{
		private readonly static Vector2 Window_Size = new(720, 640);
		private readonly static Vector2 Image_Size = new(54, 54);
		private readonly static Vector4 Alert_Color = new(208 / 255f, 177 / 255f, 50 / 255f, 1);
		private const int Item_width = 190, Item_Interval = 5;

		private bool firstDraw = false;
		private int editIndex = -1, moreEditIndex = -1;
		private string nameLabel = "", priceLabel = "";
		private readonly List<PresetItem> itemList;
		private bool visible = false;
		private bool changeOpcode = false;
		/// <summary>
		/// 正在从GitHub获取Opcode
		/// </summary>
		private bool gettingOpcodeFromOpcodeRepo = false;

		private TextureWrap? FailureImage => PluginUI.GetIcon(784);
		private readonly TradeBuddy TradeBuddy;
		private Configuration Config => TradeBuddy.Configuration;
		public Setting(TradeBuddy tradeBuddy) {
			this.TradeBuddy = tradeBuddy;
			itemList = tradeBuddy.Configuration.PresetItemList;
		}

		public void Show() {
			visible = true;
		}
		public void Draw() {
			if (!visible) {
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
			if (ImGui.Begin(TradeBuddy.Name + "插件设置", ref visible)) {
				if (ImGui.CollapsingHeader("基础设置", ImGuiTreeNodeFlags.DefaultOpen)) {
					ImGui.Indent();
					if (ImGui.Checkbox("显示交易窗口", ref Config.ShowTrade)) { Config.Save(); }

					ImGui.Checkbox("修改Opcode", ref changeOpcode);
					if (changeOpcode) {
						int tradeForm = Config.OpcodeOfTradeForm;
						if (ImGui.InputInt("交易窗口触发", ref tradeForm)) {
							Config.OpcodeOfTradeForm = (ushort)tradeForm;
							Config.Save();
						}
						int targetInfo = Config.OpcodeOfTradeTargetInfo;
						if (ImGui.InputInt("交易ID获取", ref targetInfo)) {
							Config.OpcodeOfTradeTargetInfo = (ushort)targetInfo;
							Config.Save();
						}
						if (gettingOpcodeFromOpcodeRepo) { ImGui.TextUnformatted("正在从GitHub上更新Opcode"); } else {
							if (ImGui.Button("从GitHub更新以下Opcode")) {
								gettingOpcodeFromOpcodeRepo = true;
								Task.Run(UpdateOpcodeFromGitHub);
							}
							if (ImGui.IsItemHovered()) { ImGui.SetTooltip("上方Opcode暂不支持从GitHub获取"); }
						}
						int inventoryModifyHandler = Config.OpcodeOfInventoryModifyHandler;
						if (ImGui.InputInt("InventoryModifyHandler", ref inventoryModifyHandler)) {
							Config.OpcodeOfInventoryModifyHandler = (ushort)inventoryModifyHandler;
							Config.Save();
						}
						int itemInfo = Config.OpcodeOfItemInfo;
						if (ImGui.InputInt("ItemInfo", ref itemInfo)) {
							Config.OpcodeOfItemInfo = (ushort)itemInfo;
							Config.Save();
						}
						int currencyCrystalInfo = Config.OpcodeOfCurrencyCrystalInfo;
						if (ImGui.InputInt("CurrencyCrystalInfo", ref currencyCrystalInfo)) {
							Config.OpcodeOfCurrencyCrystalInfo = (ushort)currencyCrystalInfo;
							Config.Save();
						}
						int updateInventorySlot = Config.OpcodeOfUpdateInventorySlot;
						if (ImGui.InputInt("UpdateInventorySlot", ref updateInventorySlot)) {
							Config.OpcodeOfUpdateInventorySlot = (ushort)updateInventorySlot;
							Config.Save();
						}
					}

					ImGui.Unindent();
				}

				if (ImGui.CollapsingHeader("预期价格")) {

					#region 按钮块
					//添加预期的按钮
					if (Utils.DrawIconButton(FontAwesomeIcon.Plus, -1)) {
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
					if (Utils.DrawIconButton(FontAwesomeIcon.Trash, -1)) {
						itemList.Clear();
						Config.Save();
					}
					if (ImGui.IsItemHovered()) { ImGui.SetTooltip("删除所有项目"); }

					//手动刷新价格
					ImGui.SameLine();
					if (Utils.DrawIconButton(FontAwesomeIcon.Sync, -1)) { itemList.ForEach(item => item.UpdateMinPrice()); }
					if (ImGui.IsItemHovered()) { ImGui.SetTooltip("重新获取所有价格(数据来自Universalis)"); }

					//导出到剪贴板
					ImGui.SameLine();
					if (Utils.DrawIconButton(FontAwesomeIcon.Upload, -1)) { ImGui.SetClipboardText(string.Join('\n', itemList.Select(i => i.ToString()))); }
					if (ImGui.IsItemHovered()) { ImGui.SetTooltip("导出到剪贴板"); }

					//从剪贴板导入
					ImGui.SameLine();
					if (Utils.DrawIconButton(FontAwesomeIcon.Download, -1)) {
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
					if (ImGui.IsItemHovered()) { ImGui.SetTooltip("从剪贴板导入"); }

					ImGui.SameLine();
					ImGui.TextDisabled("(?)");
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("双击鼠标左键物品编辑物品名，单击鼠标右键打开详细编辑\n不同价格方案间以;分割\n当最低价低于设定价格时，标黄进行提醒");
					#endregion

					//添加or编辑预期中
					if (editIndex != -1 && editIndex < itemList.Count) {
						bool save = false;
						//保存设置
						ImGui.SameLine();
						if (Utils.DrawIconButton(FontAwesomeIcon.Check, -1) && !string.IsNullOrEmpty(nameLabel)) { save = true; }

						//取消编辑
						ImGui.SameLine();
						if (Utils.DrawIconButton(FontAwesomeIcon.Times, -1)) { editIndex = -1; }

						ImGui.InputText("名字", ref nameLabel, 256, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) { save = true; }

						ImGui.InputText("价格", ref priceLabel, 1022564, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) { save = true; }

						int current_index = -1;
						string[] items = SearchName(nameLabel).ToArray();
						if (ImGui.ListBox("##候选表", ref current_index, items, items.Length, 3)) { nameLabel = items[current_index]; }

						if (save && Config.PresetItemDictionary.ContainsKey(nameLabel) && editIndex != Config.PresetItemDictionary[nameLabel]) {
							Chat.PrintError("物品与已有设定重复，无法添加");
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

					int rowIndex = 0;
					for (int i = 0; i < itemList.Count; i++) {
						if (ImGui.GetColumnWidth() < (Item_width + Item_Interval) * (rowIndex + 1) + 8) { rowIndex = 0; }

						if (rowIndex > 0) { ImGui.SameLine(rowIndex * (Item_width + Item_Interval) + 8); }
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
					TextureWrap? texture = PluginUI.GetIcon(item.iconId, item.quality);
					if (texture != null)
						ImGui.Image(texture.ImGuiHandle, Image_Size);
					ImGui.SameLine();
				} else {
					if (FailureImage != null)
						ImGui.Image(FailureImage.ImGuiHandle, Image_Size);
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
				if (Utils.DrawIconButton(FontAwesomeIcon.Edit, -moreEditIndex - 1)) {
					editIndex = moreEditIndex;
					nameLabel = itemList[moreEditIndex].ItemName;
					priceLabel = itemList[moreEditIndex].GetPriceStr();
					ImGui.CloseCurrentPopup();
				}

				// 重新获取当前物品的最低价格
				ImGui.SameLine();
				if (Utils.DrawIconButton(FontAwesomeIcon.Sync, -moreEditIndex - 2)) {
					itemList[moreEditIndex].UpdateMinPrice();
					ImGui.CloseCurrentPopup();
				}
				// 删除当前物品
				ImGui.SameLine();
				if (Utils.DrawIconButton(FontAwesomeIcon.Trash, -moreEditIndex - 3)) {
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
		private static List<string> SearchName(string name, bool onlyTradable = true) {
			var resultList = new List<string>();
			if (!string.IsNullOrEmpty(name)) {
				List<string>? tradeList;
				if (onlyTradable) {
					tradeList = DalamudInterface.DataManager.GetExcelSheet<Item>()?.Where(i => i.Name.ToString().Contains(name) && !i.IsUntradable).OrderByDescending(i => i.RowId).Select(i => i.Name.RawString).ToList();
				} else {
					tradeList = DalamudInterface.DataManager.GetExcelSheet<Item>()?.Where(i => i.Name.ToString().Contains(name)).OrderByDescending(i => i.RowId).Select(i => i.Name.RawString).ToList();
				}
				tradeList?.Where(i => i.StartsWith(name)).ToList().ForEach(i => resultList.Add(i));
				tradeList?.Where(i => !i.StartsWith(name)).ToList().ForEach(i => resultList.Add(i));

			}
			return resultList;
		}
		public void Dispose() { }
		/// <summary>
		/// 从GitHub上获取部分Opcode
		/// </summary>
		private async void UpdateOpcodeFromGitHub() {
			try {
				var res = await Opcode.GetOpcodesFromGitHub();
				if (res == null) {
					Chat.PrintWarning("无法从GitHub获取Opcode，返回为空");
				} else {
					Opcodes? opcodeInfo;
					if (DalamudInterface.ClientState.ClientLanguage == Dalamud.ClientLanguage.ChineseSimplified) {
						opcodeInfo = res.FirstOrDefault(i => i.Region.Equals("CN"));
					} else {
						opcodeInfo = res.FirstOrDefault(i => i.Region.Equals("Global"));
					}
					if (opcodeInfo == null) {
						Chat.PrintError("无法找到对应服务器的Opcode列表");
					} else {
						var client = opcodeInfo.Lists.ClientZoneIpcType;
						var server = opcodeInfo.Lists.ServerZoneIpcType;
						var inventoryModifyHandler = client.FirstOrDefault(i => i.Name.Equals("InventoryModifyHandler"))?.Opcode;
						if (inventoryModifyHandler != null) { Config.OpcodeOfInventoryModifyHandler = (ushort)inventoryModifyHandler; }

						var itemInfo = client.FirstOrDefault(i => i.Name.Equals("ItemInfo"))?.Opcode;
						if (itemInfo != null) { Config.OpcodeOfItemInfo = (ushort)itemInfo; }

						var currencyCrystalInfo = client.FirstOrDefault(i => i.Name.Equals("CurrencyCrystalInfo"))?.Opcode;
						if (currencyCrystalInfo != null) { Config.OpcodeOfCurrencyCrystalInfo = (ushort)currencyCrystalInfo; }

						var updateInventorySlot = client.FirstOrDefault(i => i.Name.Equals("UpdateInventorySlot"))?.Opcode;
						if (updateInventorySlot != null) { Config.OpcodeOfUpdateInventorySlot = (ushort)updateInventorySlot; }

						Config.Save();
						Chat.PrintLog($"已将部分Opcode更新至{opcodeInfo.Region}服务器<{opcodeInfo.Version}版本>");
					}
				}
			} catch (HttpRequestException e) {
				Chat.PrintWarning("无法从GitHub获取Opcode，请检查是否可以访问到GitHub");
				PluginLog.Error(e.ToString());
			}
			gettingOpcodeFromOpcodeRepo = false;
		}

	}
}
