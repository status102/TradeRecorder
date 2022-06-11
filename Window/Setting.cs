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
				if (ImGui.CollapsingHeader("预期"))
				{
					//if (ImGui.BeginTable("预期", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerH))
					{
						/*
						ImGui.TableNextColumn();
						ImGui.TableHeader("物品名");
						ImGui.TableSetupColumn("物品名", ImGuiTableColumnFlags.WidthStretch);

						ImGui.TableNextColumn();
						ImGui.TableHeader("数量");
						ImGui.TableSetupColumn("数量", ImGuiTableColumnFlags.None);

						ImGui.TableNextColumn();
						ImGui.TableHeader(" ");
						ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.None, 50);
						*/
						List<PresetItem> keyList = new List<PresetItem>();
						foreach (PresetItem item in Plugin.Instance.Configuration.presetList)
						{
							keyList.Add(new PresetItem() { name = item.name, price = item.price });
						}
						for (int i = 0; i < keyList.Count; i++)
						{
							//DalamudDll.ChatGui.Print(key);
							//ImGui.TableNextColumn();
							string name = new(keyList[i].name);
							string count = Convert.ToString(keyList[i].price);
							if (ImGui.InputText("道具名-" + (i + 1), ref name, 256, ImGuiInputTextFlags.CharsNoBlank))
							{
								if (Plugin.Instance.Configuration.presetItem.ContainsKey(name))
								{
									Plugin.Instance.Configuration.presetList[i].name = name + "1";
								}
								else
									Plugin.Instance.Configuration.presetList[i].name = name;
								Plugin.Instance.Configuration.Save();
								Plugin.Instance.Configuration.RefreshKeySet();
							}

							if (ImGui.InputText("金额-" + (i + 1), ref count, 256, ImGuiInputTextFlags.CharsDecimal))
							{
								try
								{
									Plugin.Instance.Configuration.presetList[i].price = Convert.ToInt32("0" + count);
								}
								catch (FormatException)
								{
									try
									{
										Plugin.Instance.Configuration.presetList[i].price = Convert.ToInt32("0" + count.Replace("-", string.Empty).Replace(",", string.Empty));
									}
									catch (FormatException)
									{
										Plugin.Instance.Configuration.presetList[i].price = 0;
									}
								}
								Plugin.Instance.Configuration.Save();
							}

							if (ImGuiComponents.IconButton(i, Dalamud.Interface.FontAwesomeIcon.Trash))
							{
								Plugin.Instance.Configuration.presetList.RemoveAt(i);
								Plugin.Instance.Configuration.Save();
								Plugin.Instance.Configuration.RefreshKeySet();

							}
							ImGui.Spacing();
						}
					}

					if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus) && !Plugin.Instance.Configuration.presetItem.ContainsKey(""))
					{
						Plugin.Instance.Configuration.presetList.Add(new PresetItem());
						Plugin.Instance.Configuration.RefreshKeySet();
					}
				}

			}
		}
	}
}
