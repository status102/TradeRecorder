using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TradeBuddy.Window
{
	public class Setting : IDisposable
	{
		/// <summary>
		/// 每个道具的图片大小
		/// </summary>
		private readonly static Vector2 IMAGE_SIZE = new(54, 54);
		private readonly static Vector4 Alert_Color = new(208 / 255f, 177 / 255f, 50 / 255f, 1);
		private const int Item_width = 190, Item_Interval = 5;
		private bool firstDraw = false;
		private int editIndex = -1, moreEditIndex = -1;
		private string nameLabel = "", priceLabel = "";
		private readonly List<Configuration.PresetItem> itemList;
		private readonly TextureWrap? failureImage = Configuration.GetIcon(19);
		private TradeBuddy tradeBuddy;
		private Configuration Config => Config;
		public Setting(TradeBuddy tradeBuddy)
		{
			this.tradeBuddy = tradeBuddy;
			itemList = tradeBuddy.Configuration.PresetItemList;
		}
		public void DrawSetting(ref bool _settingVisible)
		{
			if (!_settingVisible)
			{
				firstDraw = true;
				editIndex = -1;
				return;
			}

			//窗口启动时检查未获取价格的道具
			if (firstDraw)
			{
				itemList.ForEach(item => { if (item.minPrice == -1) item.UpdateMinPrice(); });
				firstDraw = false;
			}
			//ImGui.SetNextWindowSize(new Vector2(720, 640), ImGuiCond.Once);
			if (ImGui.Begin(tradeBuddy.Name + "插件设置", ref _settingVisible))
			{
				if (ImGui.CollapsingHeader("基础设置"))
				{
					ImGui.Indent();
					if (ImGui.Checkbox("显示监控窗口", ref Config.ShowTrade)) Config.Save();

					ImGui.SetNextItemWidth(400);
					if (ImGui.InputText("##确认交易字符串", ref Config.TradeConfirmStr, 256)) Config.Save();

					ImGui.SameLine();
					if (ImGui.Checkbox("确认交易后提示", ref Config.TradeConfirmAlert)) Config.Save();

					ImGui.SetNextItemWidth(400);
					if (ImGui.InputText("##取消交易字符串", ref Config.TradeCancelStr, 256)) Config.Save();

					ImGui.SameLine();
					if (ImGui.Checkbox("取消交易后提示", ref Config.TradeCancelAlert)) Config.Save();

					if (ImGui.Checkbox("##绘制不低于预期", ref Config.DrawRetainerSellListProper)) Config.Save();

					ImGui.SameLine();
					Vector3 sellListPriceProperColor = new(
						Config.RetainerSellListProperColor[0] / 255.0f,
						Config.RetainerSellListProperColor[1] / 255.0f,
						Config.RetainerSellListProperColor[2] / 255.0f
						);
					ImGui.SetNextItemWidth(300);
					if (ImGui.ColorEdit3("雇员出售价格不低于预期", ref sellListPriceProperColor))
					{
						Config.RetainerSellListProperColor[0] = (int)Math.Round(sellListPriceProperColor.X * 255);
						Config.RetainerSellListProperColor[1] = (int)Math.Round(sellListPriceProperColor.Y * 255);
						Config.RetainerSellListProperColor[2] = (int)Math.Round(sellListPriceProperColor.Z * 255);
						Config.Save();
					}
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(0, FontAwesomeIcon.Reply))
					{
						Array.Copy(Configuration.SellListProperDefaultColor, Config.RetainerSellListProperColor, 3);
						Config.Save();
					}
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("重置");

					if (ImGui.Checkbox("##绘制低于预期", ref Config.DrawRetainerSellListAlert)) Config.Save();
					ImGui.SameLine();
					Vector3 sellListPriceAlertColor = new(
					   Config.RetainerSellListAlertColor[0] / 255.0f,
					   Config.RetainerSellListAlertColor[1] / 255.0f,
					   Config.RetainerSellListAlertColor[2] / 255.0f
					   );
					ImGui.SetNextItemWidth(300);
					if (ImGui.ColorEdit3("雇员出售价格低于预期", ref sellListPriceAlertColor))
					{
						Config.RetainerSellListAlertColor[0] = (int)Math.Round(sellListPriceAlertColor.X * 255);
						Config.RetainerSellListAlertColor[1] = (int)Math.Round(sellListPriceAlertColor.Y * 255);
						Config.RetainerSellListAlertColor[2] = (int)Math.Round(sellListPriceAlertColor.Z * 255);
						Config.Save();
					}
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(1, FontAwesomeIcon.Reply))
					{
						Array.Copy(Configuration.SellListAlertDefaultColor, Config.RetainerSellListAlertColor, 3);
						Config.Save();
					}
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("重置");

					ImGui.Unindent();
				}

				if (ImGui.CollapsingHeader("预期价格"))
				{

					//添加预期的按钮
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Plus))
					{
						editIndex = -2;

						string clipboard = "";
						try
						{
							clipboard = ImGui.GetClipboardText().Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim().Replace("", "HQ");
						}
						catch (NullReferenceException) { }
						nameLabel = "";
						priceLabel = "0";

						foreach (char c in clipboard)
						{
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
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Trash))
					{
						itemList.Clear();
						Config.Save();
					}
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("删除所有项目");

					//手动刷新价格
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Sync)) itemList.ForEach(item => item.UpdateMinPrice());
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("重新获取所有价格(数据来自Universalis)");

					//导出到剪贴板
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Upload)) ImGui.SetClipboardText(string.Join('\n', itemList.Select(i => i.ToString())));
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("导出到剪贴板");

					//从剪贴板导入
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Download))
					{
						try
						{
							string clipboard = ImGui.GetClipboardText().Trim();
							string[] strLine = clipboard.Split('\n');
							foreach (string line in strLine)
							{
								if (!string.IsNullOrEmpty(line))
									itemList.Add(Configuration.PresetItem.ParseFromString(line));
							}
						}
						catch (NullReferenceException e)
						{
							PluginLog.Error("从剪贴板导入失败\n" + e.ToString());
						}
						Config.Save();
					}
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("从剪贴板导入");

					ImGui.SameLine();
					ImGui.TextDisabled("(?)");
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("双击鼠标左键物品编辑物品名，单击鼠标右键打开详细编辑\n不同价格方案间以;分割\n当最低价低于设定价格时，标黄进行提醒");

					//添加or编辑预期中
					if (editIndex != -1 && editIndex < itemList.Count)
					{
						bool save = false;
						//保存设置
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Check) && !string.IsNullOrEmpty(nameLabel)) save = true;

						//取消编辑
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Times)) editIndex = -1;

						ImGui.InputText("名字", ref nameLabel, 256, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) save = true;


						ImGui.InputText("价格", ref priceLabel, 1022564, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) save = true;

						var itemNameArray = new List<string>();
						if (!string.IsNullOrEmpty(nameLabel)) DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.Where(i => i.Name.ToString().Contains(nameLabel)).OrderBy(i => i.Name.ToString()).ToList().ForEach(i => itemNameArray.Add(i.Name.ToString()));

						int current_index = -1;
						string[] items = itemNameArray.ToArray();
						if (ImGui.ListBox("##候选表", ref current_index, items, itemNameArray.Count, 3))
							nameLabel = items[current_index];

						if (save && Config.PresetItemDictionary.ContainsKey(nameLabel) && editIndex != Config.PresetItemDictionary[nameLabel])
						{
							ImGui.SetTooltip("物品与已有设定重复，无法添加");
						}
						else if (save)
						{
							if (nameLabel == "")
							{
								itemList.RemoveAt(editIndex);
								Config.Save();
							}
							else
							{
								if (editIndex == -2)
								{
									Configuration.PresetItem presetItem = new();
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

					int lineIndex = 0;
					for (int i = 0; i < itemList.Count; i++)
					{
						if (ImGui.GetWindowWidth() < (Item_width + Item_Interval) * (lineIndex + 1) + 8)
						{
							lineIndex = 0;
						}
						if (lineIndex > 0) ImGui.SameLine(lineIndex * (Item_width + Item_Interval) + 8);
						lineIndex++;

						DrawItemBlock($"##item-{i}", itemList[i]);

						if (ImGui.IsItemHovered())
						{
							ImGui.BeginTooltip();
							ImGui.TextUnformatted($"物品名: {itemList[i].ItemName}");
							ImGui.TextUnformatted($"预设价格(价格/个): {itemList[i].GetPriceStr()}");
							ImGui.TextUnformatted("大区最低: ");
							ImGui.SameLine();
							string minPrice = itemList[i].minPrice switch
							{
								-1 => "获取失败",
								0 => "获取中",
								_ => $"<{itemList[i].minPriceServer}>" + String.Format("{0:0,0}", itemList[i].minPrice).TrimStart('0')
							};
							ImGui.TextUnformatted(minPrice);
							if (itemList[i].minPrice > 0) ImGui.TextUnformatted($"更新时间: {itemList[i].GetMinPriceUpdateTimeStr()}");
							ImGui.EndTooltip();
						}

						if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
						{
							editIndex = i;
							nameLabel = itemList[i].ItemName;
							priceLabel = itemList[i].GetPriceStr();
						}
						if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
						{
							ImGui.OpenPopup("##物品右键菜单");
							moreEditIndex = i;
						}
					}
				}

				if (ImGui.BeginPopup("##物品右键菜单", ImGuiWindowFlags.NoMove))
				{
					// 编辑当前物品
					if (ImGuiComponents.IconButton(moreEditIndex, FontAwesomeIcon.Edit))
					{
						editIndex = moreEditIndex;
						nameLabel = itemList[moreEditIndex].ItemName;
						priceLabel = itemList[moreEditIndex].GetPriceStr();
						ImGui.CloseCurrentPopup();
					}
					// 重新获取当前物品的最低价格
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(moreEditIndex, FontAwesomeIcon.Sync))
					{
						itemList[moreEditIndex].UpdateMinPrice();
						ImGui.CloseCurrentPopup();
					}
					// 删除当前物品
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(moreEditIndex, FontAwesomeIcon.Trash))
					{
						itemList.RemoveAt(moreEditIndex);
						Config.Save();
						editIndex = -1;
						ImGui.CloseCurrentPopup();
					}
					ImGui.EndPopup();
				}
				ImGui.End();
			}
		}
		/// <summary>
		/// 绘制单个道具的方块
		/// </summary>
		/// <param name="id">通过不重复的id来区别不同东西</param>
		/// <param name="item"></param>
		private void DrawItemBlock(string id, Configuration.PresetItem item)
		{
			if (ImGui.BeginChild(id, new(Item_width, IMAGE_SIZE.Y + 16), true))
			{
				if (item.iconId > 0)
				{
					TextureWrap? texture = Configuration.GetIcon(item.iconId, item.isHQ);
					if (texture != null) ImGui.Image(texture.ImGuiHandle, IMAGE_SIZE);
					ImGui.SameLine();
				}

				ImGui.BeginGroup();

				ImGui.TextUnformatted(item.minPriceServer);
				switch (item.minPrice)
				{
					case -1:
						ImGui.TextUnformatted("获取失败");
						break;
					case 0:
						ImGui.TextUnformatted("获取中");
						break;
					default:
						//市场出售价更低时显示黄色进行提醒
						if (item.minPrice < item.EvaluatePrice())
							ImGui.TextColored(Alert_Color, string.Format("{0:0,0}", item.minPrice).TrimStart('0'));
						else
							ImGui.TextUnformatted(string.Format("{0:0,0}", item.minPrice).TrimStart('0'));
						break;
				}

				if (string.IsNullOrEmpty(item.GetPriceStr())) ImGui.TextUnformatted("<未设置>");

				ImGui.EndGroup();

				ImGui.EndChild();
			}

		}

		public void Dispose()
		{
		}
	}
}
