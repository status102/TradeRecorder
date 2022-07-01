using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static TradeBuddy.Configuration;

namespace TradeBuddy.Window
{
	public class Setting
	{
		private readonly static Vector2 imageSize = new(60, 60);
		private readonly static Vector4 alertColor = new(208 / 255f, 177 / 255f, 50 / 255f, 1);
		private float mouseX = 0, mouseY = 0;
		private int mouseIndex = -1;
		private bool isItemClick = false;
		private static int editIndex = -1;
		private string nameText = "", priceText = "";
		private readonly List<PresetItem> itemList = Plugin.Instance.Configuration.PresetItemList;

		private readonly TextureWrap? failureImage = Configuration.GetIcon(19);
		public void DrawSetting(ref bool settingVisible)
		{
			if (!settingVisible)
			{
				editIndex = -1;
				return;
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
						nameText = "";
						priceText = "0";

						foreach (char c in clipboard)
						{
							if (c >= '0' && c <= '9')
								priceText += c;
							else
								nameText += c;
						}
						priceText = priceText.TrimStart('0');
						priceText = priceText.Length == 0 ? "0" : priceText;
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
					{
						itemList.ForEach(item => item.RefreshMinPrice());
					}
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
						if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Check) && !string.IsNullOrEmpty(nameText)) save = true;

						//取消编辑
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Times)) editIndex = -1;


						ImGui.InputText("名字", ref nameText, 1024, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) save = true;


						ImGui.InputText("价格", ref priceText, 1024, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) save = true;

						if (save && Plugin.Instance.Configuration.PresetItemDictionary.ContainsKey(nameText) && editIndex != Plugin.Instance.Configuration.PresetItemDictionary[nameText])
						{
							ImGui.SetTooltip("物品与已有设定重复，无法添加");
						}
						else if (save)
						{
							if (nameText == "")
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
								itemList[editIndex].ItemName = nameText;
								itemList[editIndex].SetPriceStr(priceText.Replace("-", string.Empty).Replace(",", string.Empty));
							}
							Plugin.Instance.Configuration.Save();
							editIndex = -1;
						}
					}

					if (ImGui.BeginTable("预期", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
					{

						//ImGui.TableSetupColumn("##Column1", ImGuiTableColumnFlags.WidthFixed);
						//ImGui.TableSetupColumn("##Column2", ImGuiTableColumnFlags.WidthFixed);
						//ImGui.TableHeadersRow();

						for (int i = 0; i < itemList.Count; i++)
						{
							//ImGui.TableNextRow(ImGuiTableRowFlags.None, 35);

							ImGui.TableNextColumn();
							ImGui.BeginGroup();


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
							ImGui.EndGroup();

							if (ImGui.IsItemHovered())
							{
								ImGui.BeginTooltip();
								ImGui.TextUnformatted($"物品名: {itemList[i].ItemName}");
								ImGui.TextUnformatted($"预设价格(价格/个): {itemList[i].GetPriceStr()}");
								ImGui.TextUnformatted("大区最低: ");
								ImGui.SameLine();
								minPrice = itemList[i].GetMinPrice() switch
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
								nameText = itemList[i].ItemName;
								priceText = itemList[i].GetPriceStr();
							}
							if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
							{
								isItemClick = true;
								mouseX = ImGui.GetMousePos().X;
								mouseY = ImGui.GetMousePos().Y;
								mouseIndex = i;
							}
						}
						ImGui.EndTable();
					}
				}
				// 判断是否还是刚刚的点击事件
				if (!ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsMouseClicked(ImGuiMouseButton.Right))
					isItemClick = false;
				//详细编辑菜单
				if (mouseIndex != -1)
				{
					ImGui.SetNextWindowPos(new Vector2(mouseX, mouseY), ImGuiCond.Always);
					ImGui.SetNextWindowBgAlpha(0.8f);
					if (ImGui.Begin("编辑", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
					{
						// 编辑当前物品
						if (ImGuiComponents.IconButton(mouseIndex, FontAwesomeIcon.Edit))
						{
#if DEBUG
							DalamudDll.ChatGui.Print($"编辑{mouseIndex}");
#endif
							editIndex = mouseIndex;
							nameText = itemList[mouseIndex].ItemName;
							priceText = itemList[mouseIndex].GetPriceStr();
							mouseIndex = -1;
						}
						bool editHovered = ImGui.IsItemHovered();
						// 重新获取当前物品的最低价格
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(mouseIndex, FontAwesomeIcon.Sync))
						{
							itemList[mouseIndex].RefreshMinPrice();
							mouseIndex = -1;
						}
						bool refreshHovered = ImGui.IsItemHovered();
						// 删除当前物品
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(mouseIndex, FontAwesomeIcon.Trash))
						{
							itemList.RemoveAt(mouseIndex);
							Plugin.Instance.Configuration.Save();
							editIndex = -1;
							mouseIndex = -1;
						}
						bool deleteHovered = ImGui.IsItemHovered();
						if (!editHovered && !refreshHovered && !deleteHovered && !isItemClick && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
						{
#if DEBUG
							DalamudDll.ChatGui.Print("关闭右键菜单");
#endif
							mouseIndex = -1;
						}
					}
				}
			}
		}

		public void Dispose()
		{
			if (failureImage != null) failureImage.Dispose();
		}
	}
}
