using Dalamud.Interface;
using Dalamud.Interface.Components;
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
		private static int editIndex = -1;
		private string name = "", price = "";
		private List<PresetItem> keyList = new();
		private bool refresh = true;

		public void DrawSetting(ref bool settingVisible)
		{
			if (!settingVisible)
			{
				if (editIndex == -1) return;
				if (string.IsNullOrEmpty(Plugin.Instance.Configuration.presetItemList[editIndex].name.Trim()))
					Plugin.Instance.Configuration.presetItemList.RemoveAt(editIndex);
				editIndex = -1;
			}

			ImGui.SetNextWindowSize(new Vector2(600, 720), ImGuiCond.Appearing);
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
					string confirmStr = Plugin.Instance.Configuration.tradeConfirmStr;
					if (ImGui.InputText("确认", ref confirmStr, 256))
					{
						Plugin.Instance.Configuration.tradeConfirmStr = confirmStr;
						Plugin.Instance.Configuration.Save();
					}
					ImGui.SameLine();
					bool confirmAlert = Plugin.Instance.Configuration.TradeConfirmAlert;
					if (ImGui.Checkbox("触发后提示", ref confirmAlert))
					{
						Plugin.Instance.Configuration.TradeConfirmAlert = confirmAlert;
						Plugin.Instance.Configuration.Save();
					}

					string cancelStr = Plugin.Instance.Configuration.tradeCancelStr;
					if (ImGui.InputText("取消", ref cancelStr, 256))
					{
						Plugin.Instance.Configuration.tradeCancelStr = cancelStr;
						Plugin.Instance.Configuration.Save();
					}

					ImGui.SameLine();
					bool cancelAlert = Plugin.Instance.Configuration.TradeCancelAlert;
					if (ImGui.Checkbox("触发后提示", ref cancelAlert))
					{
						Plugin.Instance.Configuration.TradeCancelAlert = cancelAlert;
						Plugin.Instance.Configuration.Save();
					}
					ImGui.Unindent();
				}
				if (ImGui.CollapsingHeader("预设价格"))
				{
					if (refresh)
					{
						keyList.Clear();
						foreach (PresetItem item in Plugin.Instance.Configuration.presetItemList)
						{
							if (item.iconId == -1 && item.name.Length > 0)
							{
								item.isHQ = item.name.EndsWith("HQ");
								if (item.isHQ) name = item.name.Substring(0, item.name.Length - 2);
								else name = item.name;
								var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == name);

								if (itemByName == null)
									item.iconId = 0;
								else
									item.iconId = itemByName.Icon;
							}
							keyList.Add(new PresetItem() { name = item.name, price = item.price, isHQ = item.isHQ, iconId = item.iconId });
						}
						refresh = false;
					}

					if (ImGui.BeginTable("预期", 4, /*ImGuiTableFlags.Resizable |*/ ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg))
					{
						ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 25f);
						ImGui.TableSetupColumn("物品名", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn("价格", ImGuiTableColumnFlags.WidthFixed, 60f);
						ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 80f);

						ImGui.TableHeadersRow();

						for (int i = 0; i < keyList.Count; i++)
						{
							ImGui.TableNextRow(ImGuiTableRowFlags.None, 35);

							ImGui.TableNextColumn();
							if (keyList[i].iconId > 0)
							{
								TextureWrap? texture = Configuration.GetIcon((uint)keyList[i].iconId, keyList[i].isHQ);
								if (texture != null) ImGui.Image(texture.ImGuiHandle, new Vector2(25, 25));
							}

							ImGui.TableNextColumn();
							ImGui.Text(keyList[i].name);

							ImGui.TableNextColumn();
							string priceShow = String.Format("{0:0,0.0}", keyList[i].price).TrimStart('0');
							if (priceShow.EndsWith(".0"))
							{
								priceShow = priceShow[0..^2];
							}
							ImGui.Text(priceShow.Length == 0 ? "0" : priceShow );

							ImGui.TableNextColumn();
							if (i == editIndex) continue;
							//修改预设金额
							if (ImGuiComponents.IconButton(i, FontAwesomeIcon.Pen))
							{
								editIndex = i;
								name = new(keyList[i].name);
								this.price = Convert.ToString(keyList[i].price);
							}

							ImGui.SameLine();
							//删除预设项目
							if (ImGuiComponents.IconButton(-i, FontAwesomeIcon.Trash))
							{
								Plugin.Instance.Configuration.presetItemList.RemoveAt(i);
								Plugin.Instance.Configuration.Save();
								Plugin.Instance.Configuration.RefreshKeySet();
								refresh = true;
							}
						}
						ImGui.EndTable();
					}

					//添加预设的按钮
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
					{
						editIndex = -2;

						string clipboard = "";
						try
						{
							clipboard = ImGui.GetClipboardText().Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
						}
						catch (NullReferenceException) { }
						name = "";
						price = "0";

						foreach (char c in clipboard)
							if (c >= '0' && c <= '9') price = price + c;
							else name = name + c;
						price = price.TrimStart('0');
						price = price.Length == 0 ? "0" : price;
					}

					ImGui.SameLine();
					//删除所有预设
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
					{
						Plugin.Instance.Configuration.presetItemList.Clear();
						Plugin.Instance.Configuration.Save();
						Plugin.Instance.Configuration.RefreshKeySet();
						refresh = true;
					}
					if (ImGui.IsItemHovered())
					{
						ImGui.SetTooltip("删除所有项目");
					}

					//导出到剪贴板
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Upload))
					{
						StringBuilder stringBuilder = new();
						Plugin.Instance.Configuration.presetItemList.ForEach(c => stringBuilder.AppendLine(c.ToString()));
						ImGui.SetClipboardText(stringBuilder.ToString());
						Plugin.Instance.Configuration.Save();
						Plugin.Instance.Configuration.RefreshKeySet();
						refresh = true;
					}
					if (ImGui.IsItemHovered())
					{
						ImGui.SetTooltip("导出到剪贴板");
					}

					//从剪贴板导入
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
					{
						try
						{
							string clipboard = ImGui.GetClipboardText().Trim();
							string[] strLine = clipboard.Split('\n'), str;
							float price = 0;
							foreach (string line in strLine)
							{
								if (line != null && line.Length > 0)
								{
									str = line.Replace("\r", "").Replace("\t", "").Trim().Split(',');
									if (str.Length == 2)
									{
										try
										{
											price = float.Parse(str[1]);
										}
										catch (FormatException)
										{
											price = 0;
										}
										Plugin.Instance.Configuration.presetItemList.Add(new PresetItem() { name = str[0], price = price });
									}

								}
							}
						}
						catch (NullReferenceException)
						{
							DalamudDll.ChatGui.PrintError("[" + Plugin.Instance.Name + "]导入失败");
						}
						Plugin.Instance.Configuration.Save();
						Plugin.Instance.Configuration.RefreshKeySet();
						refresh = true;
					}
					if (ImGui.IsItemHovered())
					{
						ImGui.SetTooltip("从剪贴板导入");
					}

					//添加or编辑预设中
					if (editIndex != -1 && editIndex < keyList.Count)
					{
						bool save = false;
						//保存设置
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(FontAwesomeIcon.Check) && !string.IsNullOrEmpty(name))
						{
							save = true;
						}
						//取消编辑
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
						{
							editIndex = -1;
						}

						ImGui.InputText("名字", ref name, 256, ImGuiInputTextFlags.CharsNoBlank);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13])
						{
							save = true;
						}

						ImGui.InputText("价格", ref price, 256, ImGuiInputTextFlags.CharsDecimal);
						if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13])
						{
							save = true;
						}

						if (save)
						{
							if (editIndex == -2)
							{
								// todo
								PresetItem presetItem = new();
								Plugin.Instance.Configuration.presetItemList.Add(presetItem);
								keyList.Add(presetItem);

								editIndex = Plugin.Instance.Configuration.presetItemList.Count - 1;
							}
							if (name == "")
							{
								Plugin.Instance.Configuration.presetItemList.RemoveAt(editIndex);
							}
							else
							{
								if (Plugin.Instance.Configuration.presetItemDictionary.ContainsKey(name) && editIndex != Plugin.Instance.Configuration.presetItemDictionary[name])
								{
									Plugin.Instance.Configuration.presetItemList[editIndex].name = name + "1";
								}
								else
								{
									Plugin.Instance.Configuration.presetItemList[editIndex].name = name;
								}

								try
								{
									Plugin.Instance.Configuration.presetItemList[editIndex].price = float.Parse("0" + price.Replace("-", string.Empty).Replace(",", string.Empty));
								}
								catch (FormatException)
								{
									Plugin.Instance.Configuration.presetItemList[editIndex].price = 0;
								}

								Plugin.Instance.Configuration.presetItemList[editIndex].isHQ = name.EndsWith("HQ");
								if (Plugin.Instance.Configuration.presetItemList[editIndex].isHQ) name = name.Substring(0, name.Length - 2);
								var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == name);
								if (itemByName == null)
									Plugin.Instance.Configuration.presetItemList[editIndex].iconId = 0;
								else
									Plugin.Instance.Configuration.presetItemList[editIndex].iconId = itemByName.Icon;
							}
							Plugin.Instance.Configuration.Save();
							Plugin.Instance.Configuration.RefreshKeySet();
							editIndex = -1;
							refresh = true;
						}

					}
				}
			}
		}
	}
}
