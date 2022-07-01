using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static TradeBuddy.Configuration;

namespace TradeBuddy
{
	public class Trade
	{
		public class Item
		{
			public long iconId;
			public bool isHQ = false;
			public string name = "";
			public int count = 0;
			public int price = 0;

			public Dictionary<int, int> priceList = new();
		}
		private readonly static Vector4[] color = new Vector4[] { new Vector4(1, 1, 1, 1), new Vector4(0, 1, 0, 1), new Vector4(1, 1, 0, 1) };
		private readonly static float[] tableWidth = new float[] { 20, -1, 12, 150 };
		private readonly static int width = 480, height = 600;
		private byte[] byteBuffer = new byte[100];
		private int giveGil = 0, receiveGil = 0;
		private string tradeTarget = "";
		private Item[] giveItem = new Item[5] { new Item(), new Item(), new Item(), new Item(), new Item() };
		private Item[] receiveItem = new Item[5] { new Item(), new Item(), new Item(), new Item(), new Item() };

		public unsafe void DrawTrade(bool tradeVisible, ref bool tradeOnceVisible, ref bool finalCheck, ref bool historyVisible, ref bool settingVisivle)
		{
			var tradeAddess = DalamudDll.GameGui.GetAddonByName("Trade", 1);
			if (tradeAddess == IntPtr.Zero)
			{
				tradeOnceVisible = true;//交易窗口关闭后重置单次关闭设置
				finalCheck = false;//最终确认时取消
				return;
			}
			if (!tradeOnceVisible || !tradeVisible) return;
			if (Plugin.Instance.PluginUi.atkArrayDataHolder == null || Plugin.Instance.PluginUi.atkArrayDataHolder->StringArrayCount < 10)
			{
				return;
			}
			var trade = (AtkUnitBase*)tradeAddess;
			if (trade->UldManager.NodeListCount <= 0) return;
			if (trade->UldManager.LoadedState != 3) return;//等待交易窗口加载完毕

			ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(trade->X - width, trade->Y), ImGuiCond.Appearing);
			if (ImGui.Begin("玩家交易", ref tradeOnceVisible, ImGuiWindowFlags.NoCollapse))
			{
				string[] header = new string[] { "", "物品", "数量", "预期金额" };
				var receiveTarget = trade->UldManager.NodeList[20]->GetAsAtkTextNode()->NodeText;
				var receiveMoney = trade->UldManager.NodeList[6]->GetAsAtkTextNode();
				var receiveChecked = trade->UldManager.NodeList[31]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode()->AtkResNode.ScaleY == 1;

				AtkResNode*[] receiveResNode = new AtkResNode*[]
				{
					trade->UldManager.NodeList[19],
					trade->UldManager.NodeList[18],
					trade->UldManager.NodeList[17],
					trade->UldManager.NodeList[16],
					trade->UldManager.NodeList[15]
				};

				var giveMoney = trade->UldManager.NodeList[24]->GetAsAtkComponentNode()->Component->UldManager.NodeList[3]->GetAsAtkTextNode();

				//var giveItem1Back = trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]->GetAsAtkImageNode();
				//var giveItem1Pic = trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode();
				var giveChecked = trade->UldManager.NodeList[32]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode()->AtkResNode.ScaleY == 1;

				AtkResNode*[] giveResNode = new AtkResNode*[]
				{
					trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[29]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[28]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[27]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[26]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]
				};

				if (giveChecked)
					ImGuiComponents.TextWithLabel("<--", "  已确认");
				else
					ImGuiComponents.TextWithLabel("<--", "");

				ImGui.SameLine(ImGui.GetColumnWidth() - 90);
				ImGui.TextDisabled("(?)");
				if (ImGui.IsItemHovered())
				{
					ImGui.BeginTooltip();
					ImGui.TextUnformatted("预期金额：");
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[0], "左键单击复制到剪贴板");

					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[0], "“---”为未设定预期金额");

					//绿色为设定HQ但交易NQ
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[1], "设定了HQ的预期金额，但交易的是NQ物品");
					//黄色为设定NQ但交易HQ
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[2], "设定了NQ的预期金额，但交易的是HQ物品");

					ImGui.EndTooltip();
				}

				ImGui.AlignTextToFramePadding();
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.History)) historyVisible = true;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog)) settingVisivle = true;

				try
				{
					DrowTradeTable(ref giveGil, 0, header, giveResNode, giveMoney, ref giveItem);
				}
				catch (Exception e)
				{
					DalamudDll.ChatGui.PrintError(e.ToString());
				}

				ImGui.Spacing();
				tradeTarget = receiveTarget.ToString();

				if (receiveChecked)
					ImGuiComponents.TextWithLabel(tradeTarget + " -->", "  已确认");
				else
					ImGuiComponents.TextWithLabel(tradeTarget + " -->", "");
				try
				{
					DrowTradeTable(ref receiveGil, 5, header, receiveResNode, receiveMoney, ref receiveItem);
				}
				catch (Exception e)
				{
					DalamudDll.ChatGui.PrintError(e.ToString());
				}

				if (receiveChecked && giveChecked)//双方交易确认，进入最终确认状态
				{
					var selectYesno = DalamudDll.GameGui.GetAddonByName("SelectYesno", 1);
					if (selectYesno != IntPtr.Zero && ((AtkUnitBase*)selectYesno)->UldManager.LoadedState == 3)
					{
						//var yesButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[11];
						//var noButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[8];
						finalCheck = true;
					}
				}
			}
			ImGui.End();
		}

		//绘制交易物品表格
		private unsafe void DrowTradeTable(ref int gil, int offset, String[] headerText, AtkResNode*[] atkResNodeList, AtkTextNode* gilTextNode, ref Item[] itemArray)
		{
			ImGui.BeginTable("交易栏", headerText.Length, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);

			for (int i = 0; i < headerText.Length; i++)
			{
				if (tableWidth.Length > i)
					if (tableWidth[i] >= 0)
						ImGui.TableSetupColumn(headerText[i], ImGuiTableColumnFlags.WidthFixed, tableWidth[i]);
					else
						ImGui.TableSetupColumn(headerText[i], ImGuiTableColumnFlags.WidthStretch);
			}
			ImGui.TableHeadersRow();

			for (int i = 0; i < atkResNodeList.Length; i++)
			{
				ImGui.TableNextRow(ImGuiTableRowFlags.None, tableWidth[0]);

				if (itemArray[i] == null) itemArray[i] = new Item();

				AtkResNode* resNode = atkResNodeList[i];
				long iconId = ((AtkComponentIcon*)resNode->GetComponent())->IconId;

				//放入交易格子中再取消时，iconId会残留为之前的数值
				if (!resNode->IsVisible || iconId < 1)
				{
					itemArray[i] = new();
					continue;
				}

				string iconIdStr = Convert.ToString(iconId);

				//判断HQ，iconId以10开头的为HQ
				if (iconIdStr.StartsWith("10"))
				{
					iconId = Convert.ToInt64(iconIdStr.Substring(2));

					itemArray[i].iconId = iconId;
					itemArray[i].isHQ = true;
				}
				else
				{
					iconId = Convert.ToInt64(iconIdStr);
					itemArray[i].iconId = iconId;
					itemArray[i].isHQ = false;
				}
				var image = Configuration.GetIcon((uint)itemArray[i].iconId, itemArray[i].isHQ);

				byte* bytePtr = Plugin.Instance.PluginUi.atkArrayDataHolder->StringArrays[9]->StringArray[offset + i];

				Array.Fill<byte>(byteBuffer, 0);

				int len = 0;
				for (int index = 0; index < 99 && bytePtr[index] != 0; index++)
				{
					len = index + 1;
					byteBuffer[index] = bytePtr[index];
				}
				if (len <= 24)
				{
					itemArray[i] = new();
					continue;
				}

				byte[] strBuffer = new byte[len - 24];
				//前面抛弃14字节，后面抛弃10字节
				Array.Copy(byteBuffer, 14, strBuffer, 0, len - 24);

				var strName = Encoding.UTF8.GetString(strBuffer).TrimEnd('');
				if (itemArray[i].name != strName)
				{
					itemArray[i].name = strName;
					itemArray[i].price = 0;
				}


				if (itemArray[i].isHQ) itemArray[i].name += "HQ";

				ImGui.TableNextColumn();
				if (image != null) ImGui.Image(image.ImGuiHandle, new Vector2(tableWidth[0], tableWidth[0]));

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(itemArray[i].name);
				if(ImGui.IsItemHovered())ImGui.SetTooltip("预设：" + Plugin.Instance.Configuration.PresetItemList[i].GetPriceStr());

				string presetPriceName = itemArray[i].name;
				//string presetPriceName = Plugin.Instance.PluginUi.atkArrayDataHolder->StringArrays[9]->;

				int priceType = 0;//0-默认， 1-设定HQ交易NQ， 2-设定NQ交易HQ
				if (Plugin.Instance.Configuration.PresetItemDictionary.ContainsKey(itemArray[i].name))
				{ }
				else if (itemArray[i].isHQ && Plugin.Instance.Configuration.PresetItemDictionary.ContainsKey(itemArray[i].name[0..^2]))
				{
					priceType = 2;
					presetPriceName = itemArray[i].name[0..^2];
				}
				else if (Plugin.Instance.Configuration.PresetItemDictionary.ContainsKey(itemArray[i].name + "HQ"))
				{
					priceType = 1;
					presetPriceName = itemArray[i].name + "HQ";
				}

				itemArray[i].priceList = new();
				foreach (Configuration.PresetItem presetItem in Plugin.Instance.Configuration.PresetItemList)
					if (presetPriceName == presetItem.ItemName)
					{
						itemArray[i].priceList = presetItem.PriceList;
						break;
					}

				int count;
				string countStr = ((AtkComponentIcon*)resNode->GetComponent())->QuantityText->NodeText.ToString().Replace(",", "").Trim();
				if (countStr == null || countStr.Length == 0)
					count = 1;
				else
					count = Convert.ToInt32("0" + countStr.Replace(",", string.Empty));
				//如果数量刷新则重置计算价格
				if (itemArray[i].count != count)
				{
					itemArray[i].count = count;
					itemArray[i].price = 0;
				}
				ImGui.TableNextColumn();
				ImGui.Text(Convert.ToString(count));

				ImGui.TableNextColumn();
				if (itemArray[i].priceList.Count == 0)
				{
					ImGui.TextColored(color[priceType], "---");
					itemArray[i].price = 0;
				}
				else
				{
					if (itemArray[i].price == 0)
					{
						if (Plugin.Instance.Configuration.StrictMode)
						{
							var countList = itemArray[i].priceList.Keys.ToList();
							countList.Sort(PresetItem.Sort);
							foreach (int num in countList)
							{
								if (itemArray[i].count / num * num == itemArray[i].count)
								{
									itemArray[i].price = itemArray[i].count / num * itemArray[i].priceList[num];
									break;
								}
							}
						}
						else
						{
							// todo 计算非严格模式下价格计算，没辙
						}
					}
					ImGui.TextColored(color[priceType], String.Format("{0:0,0}", itemArray[i].price).TrimStart('0'));
					if (ImGui.IsItemClicked()) ImGui.SetClipboardText(String.Format("{0:0,0}", itemArray[i].price).TrimStart('0'));
				}
			}

			ImGui.TableNextRow(ImGuiTableRowFlags.None, tableWidth[0]);
			ImGui.TableNextColumn();

			ImGuiScene.TextureWrap? gilImage = Configuration.GetIcon(65002, false);
			if (gilImage != null) ImGui.Image(gilImage.ImGuiHandle, new Vector2(tableWidth[0], tableWidth[0]));

			ImGui.TableNextColumn();
			ImGui.TextUnformatted("金币");

			ImGui.TableNextColumn();
			ImGui.Text(gilTextNode->NodeText.ToString());

			gil = Convert.ToInt32("0" + gilTextNode->NodeText.ToString().Replace(",", String.Empty).Trim());

			float sum = 0;
			ImGui.TableNextColumn();
			for (int i = 0; i < 5; i++)
			{
				if (itemArray[i].price != 0) sum += itemArray[i].price;
			}
			sum += gil;
			ImGui.Text(String.Format("{0:0,0}", sum).TrimStart('0'));
			if (ImGui.IsItemHovered()) ImGui.SetTooltip("包含金币在内的全部金额");
			if (ImGui.IsItemClicked()) ImGui.SetClipboardText(String.Format("{0:0,0}", sum).TrimStart('0'));
			ImGui.EndTable();
		}

		public void MessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (Plugin.Instance.PluginUi.finalCheck && type == XivChatType.SystemMessage)
			{
				//Type：SystemMessage；sid：0；sender：；msg：交易完成。；isHand：False
				//Type：SystemMessage；sid：0；sender：；msg：交易取消。；isHand：False

				if (message.TextValue == Plugin.Instance.Configuration.TradeConfirmStr)
				{
					if (Plugin.Instance.Configuration.TradeConfirmAlert)
					{
						DalamudDll.ChatGui.Print("[" + Plugin.Instance.Name + "]交易成功");
					}
					List<KeyValuePair<string, int>> giveItemList = new(), receiveItemList = new();
					foreach (Item item in giveItem)
					{
						if (item != null && item.count > 0)
						{
							giveItemList.Add(new KeyValuePair<string, int>(item.name, item.count));
						}
					}
					foreach (Item item in receiveItem)
					{
						if (item != null && item.count > 0)
						{
							receiveItemList.Add(new KeyValuePair<string, int>(item.name, item.count));
						}
					}
					Plugin.Instance.PluginUi.History.PushTradeHistory(tradeTarget, giveGil, receiveGil, giveItemList, receiveItemList);
				}
				else if (message.TextValue == Plugin.Instance.Configuration.TradeCancelStr)
				{
					if (Plugin.Instance.Configuration.TradeCancelAlert)
					{
						DalamudDll.ChatGui.PrintError("[" + Plugin.Instance.Name + "]交易取消");
					}
					List<KeyValuePair<string, int>> giveItemList = new(), receiveItemList = new();
					foreach (Item item in giveItem)
					{
						if (item != null && item.count > 0)
						{
							giveItemList.Add(new KeyValuePair<string, int>(item.name, item.count));
						}
					}
					foreach (Item item in receiveItem)
					{
						if (item != null && item.count > 0)
						{
							receiveItemList.Add(new KeyValuePair<string, int>(item.name, item.count));
						}
					}
					Plugin.Instance.PluginUi.History.PushTradeHistory(false, tradeTarget, giveGil, receiveGil, giveItemList, receiveItemList);
				}
			}
		}

	}
}
