using Dalamud.Interface.Components;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using static TradeBuddy.Configuration;

namespace TradeBuddy.Window
{
	public class Setting
	{
		private static int editIndex = -1;
		public static void DrawSetting(ref bool settingVisible)
		{
			if (!settingVisible) return;

			ImGui.SetNextWindowSize(new Vector2(480, 600), ImGuiCond.Appearing);
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
				//if (ImGui.CollapsingHeader("预设价格"))
				{
					List<PresetItem> keyList = new List<PresetItem>();
					foreach (PresetItem item in Plugin.Instance.Configuration.presetList)
					{
						keyList.Add(new PresetItem() { name = item.name, price = item.price });
					}

					if (ImGui.BeginTable("预期", 4, /*ImGuiTableFlags.Resizable |*/ ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg))
					{
						//ImGui.TableNextColumn();

						ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 25f);
						ImGui.TableSetupColumn("物品名", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn("价格", ImGuiTableColumnFlags.WidthFixed, 60f);
						ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 80f);

						ImGui.TableHeadersRow();

						for (int i = 0; i < keyList.Count; i++)
						{
							//DalamudDll.ChatGui.Print(key);
							ImGui.TableNextRow();

							ImGui.TableNextColumn();

							ImGui.TableNextColumn();
							ImGui.Text(keyList[i].name);

							ImGui.TableNextColumn();
							ImGui.Text(String.Format("{0:0,0}", keyList[i].price).TrimStart('0'));

							ImGui.TableNextColumn();
							if (ImGuiComponents.IconButton(i, Dalamud.Interface.FontAwesomeIcon.Pen))
								editIndex = i;
							ImGui.SameLine();
							if (ImGuiComponents.IconButton(-i, Dalamud.Interface.FontAwesomeIcon.Trash))
							{
								Plugin.Instance.Configuration.presetList.RemoveAt(i);
								Plugin.Instance.Configuration.Save();
								Plugin.Instance.Configuration.RefreshKeySet();
							}
						}
						ImGui.EndTable();
					}

					if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus) && !Plugin.Instance.Configuration.presetItem.ContainsKey(""))
					{
						Plugin.Instance.Configuration.presetList.Add(new PresetItem());
						Plugin.Instance.Configuration.RefreshKeySet();
						editIndex = keyList.Count - 1;
					}
					if (editIndex != -1 && editIndex < keyList.Count)
					{
						ImGui.SameLine();
						if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Check))
							editIndex = -1;


						string name = new(keyList[editIndex].name);
						string count = Convert.ToString(keyList[editIndex].price);
						if (ImGui.InputText("名字", ref name, 256, ImGuiInputTextFlags.CharsNoBlank))
						{
							if (Plugin.Instance.Configuration.presetItem.ContainsKey(name))
							{
								Plugin.Instance.Configuration.presetList[editIndex].name = name + "1";
							}
							else
								Plugin.Instance.Configuration.presetList[editIndex].name = name;
							Plugin.Instance.Configuration.Save();
							Plugin.Instance.Configuration.RefreshKeySet();
						}

						if (ImGui.InputText("价格", ref count, 256, ImGuiInputTextFlags.CharsDecimal))
						{
							try
							{
								Plugin.Instance.Configuration.presetList[editIndex].price = Convert.ToInt32("0" + count);
							}
							catch (FormatException)
							{
								try
								{
									Plugin.Instance.Configuration.presetList[editIndex].price = Convert.ToInt32("0" + count.Replace("-", string.Empty).Replace(",", string.Empty));
								}
								catch (FormatException)
								{
									Plugin.Instance.Configuration.presetList[editIndex].price = 0;
								}
							}
							Plugin.Instance.Configuration.Save();
						}

					}

				}

			}
		}
	}
}
