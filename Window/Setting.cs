using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static TradeBuddy.Configuration;

namespace TradeBuddy.Window
{
	public class Setting
	{
		/// <summary>
		/// 每个道具的图片大小
		/// </summary>
		private readonly static Vector2 imageSize = new(54, 54);
		private readonly static Vector4 alertColor = new(208 / 255f, 177 / 255f, 50 / 255f, 1);
		private const int itemGroupWidth = 190, itemGroupTer = 5;
		private bool firstDraw = false;
		private int editIndex = -1, moreEditIndex = -1;
		private string nameLabel = "", priceLabel = "";
		private readonly List<PresetItem> itemList = Plugin.Instance.Configuration.PresetItemList;

		private readonly TextureWrap? failureImage = Configuration.GetIcon(19);
		public void DrawSetting(ref bool settingVisible)
		{
			if (!settingVisible)
			{
				firstDraw = true;
				editIndex = -1;
				return;
			}
			
			//窗口启动时检查未获取价格的道具
			if (firstDraw)
			{
				itemList.ForEach(item => { if (item.GetMinPrice() == -1) item.UpdateMinPrice(); });
				firstDraw = false;
			}
			//ImGui.SetNextWindowSize(new Vector2(720, 640), ImGuiCond.Once);
			if (ImGui.Begin(Plugin.Instance.Name + "插件设置", ref settingVisible))
			{
				if (ImGui.CollapsingHeader("基础设置"))
				{
					ImGui.Indent();
					bool showTrade = Plugin.Instance.Configuration.ShowTrade;
					if (ImGui.Checkbox("显示监控窗口", ref showTrade))
					{
						Plugin.Instance.Configuration.ShowTrade = showTrade;
						Plugin.Instance.Configuration.Save();
					}

					string confirmStr = Plugin.Instance.Configuration.TradeConfirmStr;
					ImGui.SetNextItemWidth(400);
					if (ImGui.InputText("##确认交易字符串", ref confirmStr, 256))
					{
						Plugin.Instance.Configuration.TradeConfirmStr = confirmStr;
						Plugin.Instance.Configuration.Save();
					}

					ImGui.SameLine();
					bool confirmAlert = Plugin.Instance.Configuration.TradeConfirmAlert;
					if (ImGui.Checkbox("确认交易后提示", ref confirmAlert))
					{
						Plugin.Instance.Configuration.TradeConfirmAlert = confirmAlert;
						Plugin.Instance.Configuration.Save();
					}

					string cancelStr = Plugin.Instance.Configuration.TradeCancelStr;
					ImGui.SetNextItemWidth(400);
					if (ImGui.InputText("##取消交易字符串", ref cancelStr, 256))
					{
						Plugin.Instance.Configuration.TradeCancelStr = cancelStr;
						Plugin.Instance.Configuration.Save();
					}

					ImGui.SameLine();
					bool cancelAlert = Plugin.Instance.Configuration.TradeCancelAlert;
					if (ImGui.Checkbox("取消交易后提示", ref cancelAlert))
					{
						Plugin.Instance.Configuration.TradeCancelAlert = cancelAlert;
						Plugin.Instance.Configuration.Save();
					}

					bool drawProPerColor = Plugin.Instance.Configuration.DrawRetainerSellListProper;
					if (ImGui.Checkbox("##绘制不低于预期", ref drawProPerColor))
					{
						Plugin.Instance.Configuration.DrawRetainerSellListProper = drawProPerColor;
						Plugin.Instance.Configuration.Save();
					}
					ImGui.SameLine();
					Vector3 sellListPriceProperColor = new(
						Plugin.Instance.Configuration.RetainerSellListProperColor[0] / 255.0f,
						Plugin.Instance.Configuration.RetainerSellListProperColor[1] / 255.0f,
						Plugin.Instance.Configuration.RetainerSellListProperColor[2] / 255.0f
						);
					ImGui.SetNextItemWidth(300);
					if (ImGui.ColorEdit3("雇员出售价格不低于预期", ref sellListPriceProperColor))
					{
						Plugin.Instance.Configuration.RetainerSellListProperColor[0] = (int)Math.Round(sellListPriceProperColor.X * 255);
						Plugin.Instance.Configuration.RetainerSellListProperColor[1] = (int)Math.Round(sellListPriceProperColor.Y * 255);
						Plugin.Instance.Configuration.RetainerSellListProperColor[2] = (int)Math.Round(sellListPriceProperColor.Z * 255);
						Plugin.Instance.Configuration.Save();
					}
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(0, FontAwesomeIcon.Reply))
					{
						Array.Copy(Plugin.Instance.Configuration.DefaultSellListProperColor, Plugin.Instance.Configuration.RetainerSellListProperColor, 3);
						Plugin.Instance.Configuration.Save();
					}
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("重置");

					bool drawAlertColor = Plugin.Instance.Configuration.DrawRetainerSellListAlert;
					if (ImGui.Checkbox("##绘制低于预期", ref drawAlertColor))
					{
						Plugin.Instance.Configuration.DrawRetainerSellListAlert = drawAlertColor;
						Plugin.Instance.Configuration.Save();
					}
					ImGui.SameLine();
					Vector3 sellListPriceAlertColor = new(
					   Plugin.Instance.Configuration.RetainerSellListAlertColor[0] / 255.0f,
					   Plugin.Instance.Configuration.RetainerSellListAlertColor[1] / 255.0f,
					   Plugin.Instance.Configuration.RetainerSellListAlertColor[2] / 255.0f
					   );
					ImGui.SetNextItemWidth(300);
					if (ImGui.ColorEdit3("雇员出售价格低于预期", ref sellListPriceAlertColor))
					{
						Plugin.Instance.Configuration.RetainerSellListAlertColor[0] = (int)Math.Round(sellListPriceAlertColor.X * 255);
						Plugin.Instance.Configuration.RetainerSellListAlertColor[1] = (int)Math.Round(sellListPriceAlertColor.Y * 255);
						Plugin.Instance.Configuration.RetainerSellListAlertColor[2] = (int)Math.Round(sellListPriceAlertColor.Z * 255);
						Plugin.Instance.Configuration.Save();
					}
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(1, FontAwesomeIcon.Reply))
					{
						Array.Copy(Plugin.Instance.Configuration.DefaultSellListAlertColor, Plugin.Instance.Configuration.RetainerSellListAlertColor, 3);
						Plugin.Instance.Configuration.Save();
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
						priceLabel = priceLabel.Length == 0 ? "0" : priceLabel;
					}

					ImGui.SameLine();
					//删除所有预期
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Trash))
					{
						itemList.Clear();
						Plugin.Instance.Configuration.Save();
					}
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("删除所有项目");

					ImGui.SameLine();
					//刷新价格
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Sync))
						itemList.ForEach(item => item.UpdateMinPrice());
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("重新获取所有价格(数据来自Universalis)");

					//导出到剪贴板
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Upload))
					{
						StringBuilder stringBuilder = new();
						itemList.ForEach(c => stringBuilder.AppendLine(c.ToString()));
						ImGui.SetClipboardText(stringBuilder.ToString());
					}
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
									itemList.Add(PresetItem.ParseFromString(line));
							}
						}
						catch (NullReferenceException e)
						{
							PluginLog.Error("从剪贴板导入失败\n" + e.ToString());
						}
						Plugin.Instance.Configuration.Save();
					}
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("从剪贴板导入");

					ImGui.SameLine();
					ImGui.TextDisabled("(?)");
					if (ImGui.IsItemHovered()) ImGui.SetTooltip("双击鼠标左键物品编辑物品名，单击鼠标右键打开详细编辑\n不同价格方案间以;分割");

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

						ImGui.InputText("名字", ref nameLabel, 1024, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) save = true;


						ImGui.InputText("价格", ref priceLabel, 1024, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) save = true;

						var itemNameArray = new List<string>();
						if(!string.IsNullOrEmpty(nameLabel))DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.Where(i => i.Name.ToString().Contains(nameLabel)).OrderBy(i => i.Name.ToString()).ToList().ForEach(i => itemNameArray.Add(i.Name.ToString()));

						int current_index = -1;
						string[] items = itemNameArray.ToArray();
						if (ImGui.ListBox("##候选表", ref current_index, items, itemNameArray.Count, 3))
							nameLabel = items[current_index];

						if (save && Plugin.Instance.Configuration.PresetItemDictionary.ContainsKey(nameLabel) && editIndex != Plugin.Instance.Configuration.PresetItemDictionary[nameLabel])
						{
							ImGui.SetTooltip("物品与已有设定重复，无法添加");
						}
						else if (save)
						{
							if (nameLabel == "")
							{
								itemList.RemoveAt(editIndex);
								Plugin.Instance.Configuration.Save();
							}
							else
							{
								if (editIndex == -2)
								{
									PresetItem presetItem = new();
									itemList.Add(presetItem);

									editIndex = itemList.Count - 1;
								}
								itemList[editIndex].ItemName = nameLabel;
								itemList[editIndex].SetPriceStr(priceLabel.Replace("-", string.Empty).Replace(",", string.Empty));
							}
							Plugin.Instance.Configuration.Save();
							editIndex = -1;
						}
					}

					int lineIndex = 0;
					for (int i = 0; i < itemList.Count; i++)
					{
						if (ImGui.GetWindowWidth() < (itemGroupWidth + itemGroupTer) * (lineIndex + 1) + 8)
						{
							lineIndex = 0;
						}
						if (lineIndex > 0) ImGui.SameLine(lineIndex * (itemGroupWidth + itemGroupTer) + 8);
						lineIndex++;

						if (ImGui.BeginChild("##item-" + i, new(itemGroupWidth, imageSize.Y + 16), true))
						{
							if (itemList[i].GetIconId() > 0)
							{
								TextureWrap? texture = Configuration.GetIcon(itemList[i].GetIconId(), itemList[i].IsHQ());
								if (texture != null) ImGui.Image(texture.ImGuiHandle, imageSize);
							}
							else
							{
								if (failureImage != null) ImGui.Image(failureImage.ImGuiHandle, imageSize);
								else ImGui.TextUnformatted("图标获取失败");
							}

							ImGui.SameLine();

							ImGui.BeginGroup();

							string minPrice = itemList[i].GetMinPrice() switch
							{
								-1 => "获取失败",
								0 => "获取中",
								_ => $"<{itemList[i].GetMinPriceServer()}>{itemList[i].GetMinPrice()}"
							};


							ImGui.TextUnformatted(itemList[i].GetMinPriceServer());
							switch (itemList[i].GetMinPrice())
							{
								case -1:
									ImGui.TextUnformatted("获取失败");
									break;
								case 0:
									ImGui.TextUnformatted("获取中");
									break;
								default:
									//市场出售价更低时显示黄色进行提醒
									if (itemList[i].GetMinPrice() < itemList[i].EvaluatePrice())
										ImGui.TextColored(alertColor, String.Format("{0:0,0}", itemList[i].GetMinPrice()).TrimStart('0'));
									else
										ImGui.TextUnformatted(String.Format("{0:0,0}", itemList[i].GetMinPrice()).TrimStart('0'));
									break;
							}

							if (String.IsNullOrEmpty(itemList[i].GetPriceStr())) ImGui.TextUnformatted("<未设置>");

							ImGui.EndGroup();

							ImGui.EndChild();
						}

						if (ImGui.IsItemHovered())
						{
							ImGui.BeginTooltip();
							ImGui.TextUnformatted($"物品名: {itemList[i].ItemName}");
							ImGui.TextUnformatted($"预设价格(价格/个): {itemList[i].GetPriceStr()}");
							ImGui.TextUnformatted("大区最低: ");
							ImGui.SameLine();
							string minPrice = itemList[i].GetMinPrice() switch
							{
								-1 => "获取失败",
								0 => "获取中",
								_ => $"<{itemList[i].GetMinPriceServer()}>" + String.Format("{0:0,0}", itemList[i].GetMinPrice()).TrimStart('0')
							};
							ImGui.TextUnformatted(minPrice);
							if (itemList[i].GetMinPrice() > 0) ImGui.TextUnformatted($"更新时间: {itemList[i].GetMinPriceUpdateTimeStr()}");
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
						Plugin.Instance.Configuration.Save();
						editIndex = -1;
						ImGui.CloseCurrentPopup();
					}
					ImGui.EndPopup();
				}
				ImGui.End();
			}
		}

		public void Dispose()
		{
			if (failureImage != null) failureImage.Dispose();
		}
	}
}
