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
		private string name, price;
		private List<PresetItem> keyList = new List<PresetItem>();
		private bool refresh = true;

		public void DrawSetting(ref bool settingVisible)
		{
			if (!settingVisible)
			{
				if (editIndex == -1) return;
				if (string.IsNullOrEmpty(Plugin.Instance.Configuration.presetList[editIndex].name.Trim()))
					Plugin.Instance.Configuration.presetList.RemoveAt(editIndex);
				editIndex = -1;
			}

			ImGui.SetNextWindowSize(new Vector2(600, 720), ImGuiCond.Appearing);
			if (ImGui.Begin(Plugin.Instance.Name + "插件设置", ref settingVisible))
			{
				if (ImGui.CollapsingHeader("基础设置"))
				{
					ImGui.Indent();
					bool showTrade = Plugin.Instance.Configuration.ShowTrade;
					if (ImGui.Checkbox("交易时显示监控窗口", ref showTrade))
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

					string cancelStr = Plugin.Instance.Configuration.tradeCancelStr;
					if (ImGui.InputText("取消", ref cancelStr, 256))
					{
						Plugin.Instance.Configuration.tradeCancelStr = cancelStr;
						Plugin.Instance.Configuration.Save();
					}
					ImGui.Unindent();
				}
				if (ImGui.CollapsingHeader("预设价格"))
				{
					if (refresh)
					{
						keyList.Clear();
						foreach (PresetItem item in Plugin.Instance.Configuration.presetList)
						{
							DalamudDll.ChatGui.Print("refresh：" + item.name);
							if (item.iconId == -1 && item.name.Length > 0)
							{
								item.isHQ = item.name.EndsWith("HQ");
								if (item.isHQ) name = item.name.Substring(0, item.name.Length - 2);
								else name  = item.name;
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
								TextureWrap? texture = Configuration.getIcon((uint)keyList[i].iconId, keyList[i].isHQ);
								if (texture != null) ImGui.Image(texture.ImGuiHandle, new Vector2(25, 25));
							}

							ImGui.TableNextColumn();
							ImGui.Text(keyList[i].name);

							ImGui.TableNextColumn();
							ImGui.Text(String.Format("{0:0,0}", keyList[i].price).TrimStart('0'));

							ImGui.TableNextColumn();
							if (i == editIndex) continue;
							//修改预设金额
							if (ImGuiComponents.IconButton(i, FontAwesomeIcon.Pen))
							{
								editIndex = i;
								name = new(keyList[i].name);
								price = Convert.ToString(keyList[i].price);
							}

							ImGui.SameLine();
							//删除预设项目
							if (ImGuiComponents.IconButton(-i, FontAwesomeIcon.Trash))
							{
								Plugin.Instance.Configuration.presetList.RemoveAt(i);
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
						if (Plugin.Instance.Configuration.presetList[Plugin.Instance.Configuration.presetList.Count - 1].name == "")
						{
							editIndex = Plugin.Instance.Configuration.presetList.Count - 1;
							name = new(keyList[editIndex].name);
							price = Convert.ToString(keyList[editIndex].price);
						}
						else
						{
							PresetItem presetItem = new PresetItem();
							Plugin.Instance.Configuration.presetList.Add(presetItem);
							keyList.Add(presetItem);

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

							editIndex = Plugin.Instance.Configuration.presetList.Count - 1;
						}
					}

					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
					{
						Plugin.Instance.Configuration.presetList.Clear();
						Plugin.Instance.Configuration.Save();
						Plugin.Instance.Configuration.RefreshKeySet();
						refresh = true;
					}
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("删除所有项目");

					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Upload))
					{
						StringBuilder stringBuilder = new StringBuilder();
						foreach (PresetItem item in Plugin.Instance.Configuration.presetList)
							stringBuilder.Append(item.name).Append(',').Append(item.price).Append('\n');
						ImGui.SetClipboardText(stringBuilder.ToString());
						Plugin.Instance.Configuration.Save();
						Plugin.Instance.Configuration.RefreshKeySet();
						refresh = true;
					}
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("导出到剪贴板");

					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
					{
						try
						{
							string clipboard = ImGui.GetClipboardText().Trim();
							string[] strLine = clipboard.Split('\n'), str;
							int price = 0;
							foreach (string line in strLine)
							{
								if (line != null && line.Length > 0)
								{
									str = line.Replace("\r", "").Replace("\t", "").Trim().Split(',');
									if (str.Length == 2)
									{
										try
										{
											price = int.Parse(str[1]);
										}
										catch (FormatException)
										{
											price = 0;
										}
										Plugin.Instance.Configuration.presetList.Add(new PresetItem() { name = str[0], price = price });
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
						ImGui.SetTooltip("从剪贴板导入");



					//添加or编辑预设中
					if (editIndex != -1 && editIndex < keyList.Count)
					{
						//保存设置
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(FontAwesomeIcon.Check) && !string.IsNullOrEmpty(name))
						{
							if (Plugin.Instance.Configuration.presetItem.ContainsKey(name) && editIndex != Plugin.Instance.Configuration.presetItem[name])
							{
								Plugin.Instance.Configuration.presetList[editIndex].name = name + "1";
								DalamudDll.ChatGui.Print(editIndex + "-" + Plugin.Instance.Configuration.presetItem[name]);
							}
							else
								Plugin.Instance.Configuration.presetList[editIndex].name = name;

							try
							{
								Plugin.Instance.Configuration.presetList[editIndex].price = Convert.ToInt32("0" + price.Replace("-", string.Empty).Replace(",", string.Empty));
							}
							catch (FormatException)
							{
								Plugin.Instance.Configuration.presetList[editIndex].price = 0;
							}

							Plugin.Instance.Configuration.presetList[editIndex].isHQ = name.EndsWith("HQ");
							if (Plugin.Instance.Configuration.presetList[editIndex].isHQ) name = name.Substring(0, name.Length - 2);
							var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == name);
							if (itemByName == null)
								Plugin.Instance.Configuration.presetList[editIndex].iconId = 0;
							else
								Plugin.Instance.Configuration.presetList[editIndex].iconId = itemByName.Icon;

							Plugin.Instance.Configuration.Save();
							Plugin.Instance.Configuration.RefreshKeySet();
							editIndex = -1;
							refresh = true;
						}
						//取消编辑
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
						{
							if (Plugin.Instance.Configuration.presetList[editIndex].name == "")
							{
								Plugin.Instance.Configuration.presetList.RemoveAt(editIndex);
								Plugin.Instance.Configuration.Save();
								Plugin.Instance.Configuration.RefreshKeySet();
							}
							editIndex = -1;
						}

						if (ImGui.InputText("名字", ref name, 256, ImGuiInputTextFlags.CharsNoBlank))
						{
							if (Plugin.Instance.Configuration.presetItem.ContainsKey(name)) name = name + "1";
						}

						ImGui.InputText("价格", ref price, 256, ImGuiInputTextFlags.CharsDecimal);
					}
				}
			}
		}
	}
}
