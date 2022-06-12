using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using static Dalamud.Game.Gui.ChatGui;

namespace TradeBuddy
{
	public class Trade
	{
		public class Item
		{
			public long iconId;
			public bool isHQ = false;
			public string name { get; set; } = "";
			public int count { get; set; } = 0;

			public int price { get; set; } = 0;
		}
		private static Vector4[] color = new Vector4[] { new Vector4(1, 1, 1, 1), new Vector4(0, 1, 0, 1), new Vector4(1, 1, 0, 1) };
		private static float[] tableWidth = new float[] { 20, -1, 85, 120 };
		private static int width = 480, height = 600, giveGil = 0, receiveGil = 0;
		private static string tradeTarget = "";
		public static Item[] giveItem = new Item[5] { new Item(), new Item(), new Item(), new Item(), new Item() };
		public static Item[] receiveItem = new Item[5] { new Item(), new Item(), new Item(), new Item(), new Item() };

		public static OnMessageDelegate messageDelegate = (XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) =>
		  {
			  try
			  {
				  if (Plugin.Instance.PluginUi.finalCheck && type == XivChatType.SystemMessage && !isHandled)
				  {
					  //Type：SystemMessage；sid：0；sender：；msg：交易完成。；isHand：False
					  //Type：SystemMessage；sid：0；sender：；msg：交易取消。；isHand：False
					  if (message.TextValue == Plugin.Instance.Configuration.tradeConfirmStr)
					  {
						  if (Plugin.Instance.Configuration.PrintConfirmTrade) DalamudDll.ChatGui.Print("[" + Plugin.Instance.Name + "]交易成功");
						  Plugin.Instance.History.PushTradeHistory(tradeTarget, giveGil, giveItem, receiveGil, receiveItem);
					  }
					  else if (message.TextValue == Plugin.Instance.Configuration.tradeCancelStr)
						  if (Plugin.Instance.Configuration.PrintCancelTrade) DalamudDll.ChatGui.PrintError("[" + Plugin.Instance.Name + "]交易取消");
				  }
			  }
			  catch (Exception ex)
			  {
				  DalamudDll.ChatGui.PrintError(ex.ToString());
			  }
		  };
		public static unsafe void DrawTrade(bool tradeVisible, ref bool tradeOnceVisible, ref bool finalCheck, ref bool historyVisible, ref bool settingVisivle)
		{
			var tradeAddess = DalamudDll.GameGui.GetAddonByName("Trade", 1);
			if (tradeAddess == IntPtr.Zero)
			{
				tradeOnceVisible = true;//交易窗口关闭后重置单次关闭设置
				finalCheck = false;//最终确认时取消
				return;
			}
			if (!tradeOnceVisible || !tradeVisible) return;
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
				ImGui.TextColored(new Vector4(1, 1, 1, 0.5f), "(?)");
				if (ImGui.IsItemHovered())
				{
					ImGui.BeginTooltip();
					ImGui.Text("预期金额：");
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[0], "左键单击复制到剪贴板");

					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[0], "“---”为未设定预期金额");

					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[0], "“100/2,000”==>“单价/总价”");
					//绿色为设定HQ但交易NQ
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[1], "为设定了HQ的预期金额，但交易的是NQ物品");
					//黄色为设定NQ但交易HQ
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(color[2], "为设定了NQ的预期金额，但交易的是HQ物品");

					ImGui.EndTooltip();
				}

				ImGui.AlignTextToFramePadding();
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.History)) historyVisible = true;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog)) settingVisivle = true;

				try
				{
					DrowTradeTable(header, giveResNode, giveMoney, ref giveItem, ref giveGil);
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
					DrowTradeTable(header, receiveResNode, receiveMoney, ref receiveItem, ref receiveGil);
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
		private static unsafe void DrowTradeTable(String[] headerText, AtkResNode*[] atkResNodeList, AtkTextNode* gilTextNode, ref Item[] itemArray, ref int gil)
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

				// todo 测试数据
				if (itemArray[i] == null) itemArray[i] = new Item();

				AtkResNode* resNode = atkResNodeList[i];
				long iconId = ((AtkComponentIcon*)resNode->GetComponent())->IconId;

				//放入交易格子中再取消时，iconId会残留为之前的数值
				if (!resNode->IsVisible || iconId < 1)
				{
					itemArray[i].iconId = 0;
					itemArray[i].isHQ = false;
					itemArray[i].name = "";
					itemArray[i].count = 0;
					itemArray[i].price = 0;
					for (int j = 0; j < headerText.Length; j++)
					{
						ImGui.TableNextColumn();
						ImGui.Text("");
					}
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
				var image = Configuration.getIcon((uint)itemArray[i].iconId, itemArray[i].isHQ);
				var itemByIconId = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Icon == iconId);
				//iconId反查item失败
				if (itemByIconId == null)
				{
					itemArray[i].name = "";
					itemArray[i].iconId = 0;
					itemArray[i].isHQ = false;
					itemArray[i].count = 0;
					itemArray[i].price = 0;
					for (int j = 0; j < headerText.Length; j++)
					{
						ImGui.TableNextColumn();
						ImGui.Text(iconId + "-" + iconIdStr);
					}
					continue;
				}

				itemArray[i].name = itemByIconId.Name;
				if (itemArray[i].isHQ) itemArray[i].name += "HQ";

				ImGui.TableNextColumn();
				if (image != null) ImGui.Image(image.ImGuiHandle, new Vector2(tableWidth[0], tableWidth[0]));

				ImGui.TableNextColumn();
				ImGui.Text(itemArray[i].name);

				string presetPriceName = itemArray[i].name;
				itemArray[i].price = 0;
				int priceType = 0;//0-默认， 1-设定HQ交易NQ， 2-设定NQ交易HQ
				if (Plugin.Instance.Configuration.presetItem.ContainsKey(itemArray[i].name))
				{ }
				else if (itemArray[i].isHQ && Plugin.Instance.Configuration.presetItem.ContainsKey(itemArray[i].name[0..^2]))
				{
					priceType = 2;
					presetPriceName = itemArray[i].name[0..^2];
				}
				else if (Plugin.Instance.Configuration.presetItem.ContainsKey(itemArray[i].name + "HQ"))
				{
					priceType = 1;
					presetPriceName = itemArray[i].name + "HQ";
				}

				foreach (Configuration.PresetItem presetItem in Plugin.Instance.Configuration.presetList)
					if (presetPriceName == presetItem.name)
					{
						itemArray[i].price = presetItem.price;
						break;
					}

				int count;
				string countStr = ((AtkComponentIcon*)resNode->GetComponent())->QuantityText->NodeText.ToString().Replace(",", "").Trim();
				if (countStr == null || countStr.Length == 0)
					count = 1;
				else
					count = Convert.ToInt32("0" + countStr.Replace(",", string.Empty));

				itemArray[i].count = count;
				ImGui.TableNextColumn();
				ImGui.Text(Convert.ToString(count));

				ImGui.TableNextColumn();
				if (itemArray[i].price == 0)
					ImGui.TextColored(color[priceType], "---");
				else
				{
					ImGui.TextColored(color[priceType], String.Format("{0:0,0}", itemArray[i].price).TrimStart('0') + "/" + String.Format("{0:0,0}", itemArray[i].price * itemArray[i].count).TrimStart('0'));
					if (ImGui.IsItemClicked()) ImGui.SetClipboardText(String.Format("{0:0,0}", itemArray[i].price * itemArray[i].count).TrimStart('0'));
				}
			}

			ImGui.TableNextColumn();

			ImGuiScene.TextureWrap? gilImage = Configuration.getIcon(65002, false);
			if (gilImage != null) ImGui.Image(gilImage.ImGuiHandle, new Vector2(tableWidth[0], tableWidth[0]));

			ImGui.TableNextColumn();
			ImGui.Text("金币");

			ImGui.TableNextColumn();
			ImGui.Text(gilTextNode->NodeText.ToString());

			gil = Convert.ToInt32("0" + gilTextNode->NodeText.ToString().Replace(",", String.Empty).Trim());

			int sum = 0;
			ImGui.TableNextColumn();
			for (int i = 0; i < 5; i++)
			{
				if (itemArray[i].price != 0) sum += itemArray[i].count * itemArray[i].price;
			}
			sum += gil;
			ImGui.Text(String.Format("{0:0,0}", sum).TrimStart('0'));
			if (ImGui.IsItemHovered()) ImGui.SetTooltip("包含金币在内的全部金额");
			if (ImGui.IsItemClicked()) ImGui.SetTooltip(String.Format("{0:0,0}", sum).TrimStart('0'));
			ImGui.EndTable();
		}
	}
}
