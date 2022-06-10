using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace TradeBuddy
{
	public class TradeUI
	{
		public class Item
		{
			public string name { get; set; } = "";
			public int count { get; set; } = 0;

			public int price { get; set; } = 0;
		}
		private static Vector4[] color = new Vector4[] { new Vector4(1, 1, 1, 1), new Vector4(0, 1, 0, 1), new Vector4(1, 1, 0, 1) };
		private static float[] tableWidth = new float[] { -1, 80, 300 };
		private static int width = 480, giveGil = 0, receiveGil = 0;
		public static Item[] giveItem = new Item[5] { new Item(), new Item(), new Item(), new Item(), new Item() };
		public static Item[] receiveItem = new Item[5] { new Item(), new Item(), new Item(), new Item(), new Item() };

		public static unsafe void DrawTrade(ref bool tradeOnceVisible, ref bool finalCheck, ref bool historyVisible, ref bool settingVisivle)
		{
			var tradeAddess = Plugin.plugin.GameGui.GetAddonByName("Trade", 1);
			if (tradeAddess == IntPtr.Zero)
			{
				tradeOnceVisible = true;//交易窗口关闭后重置单次关闭设置
				finalCheck = false;//最终确认时取消
				return;
			}
			if (!tradeOnceVisible) return;
			var trade = (AtkUnitBase*)tradeAddess;
			if (trade->UldManager.NodeListCount <= 0) return;
			if (trade->UldManager.LoadedState != 3) return;//等待交易窗口加载完毕

			ImGui.SetNextWindowSize(new Vector2(width, 600), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(trade->X - width, trade->Y), ImGuiCond.Appearing);
			if (ImGui.Begin("玩家交易", ref tradeOnceVisible, ImGuiWindowFlags.NoCollapse))
			{
				string[] header = new string[] { "物品", "数量", "预期金额" };
				var receiveTarget = trade->UldManager.NodeList[20]->GetAsAtkTextNode()->NodeText;
				var receiveMoney = trade->UldManager.NodeList[6]->GetAsAtkTextNode();
				var receiveChecked = trade->UldManager.NodeList[31]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode()->AtkResNode.ScaleY == 1;

				AtkResNode*[] receiveArray = new AtkResNode*[]
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

				AtkResNode*[] giveArray = new AtkResNode*[]
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
					//ImGui.SetTooltip("测试");
				}

				ImGui.AlignTextToFramePadding();
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.History)) historyVisible = true;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog)) settingVisivle = true;

				DrowTradeTable(header, giveArray, giveMoney, ref giveItem, ref giveGil);

				ImGui.Spacing();

				if (receiveChecked)
					ImGuiComponents.TextWithLabel(receiveTarget.ToString() + " -->", "  已确认");
				else
					ImGuiComponents.TextWithLabel(receiveTarget.ToString() + " -->", "");
				DrowTradeTable(header, receiveArray, receiveMoney, ref receiveItem, ref receiveGil);
				if (receiveChecked && giveChecked)//双方交易确认，进入最终确认状态
				{
					var selectYesno = Plugin.plugin.GameGui.GetAddonByName("SelectYesno", 1);
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
			AtkResNode* resNode;
			long iconId;
			int count;
			string iconIdStr, itemName, countStr;

			// todo 加入物品图片
			//绘制5个道具栏
			ImGui.BeginTable("交易栏", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);

			for (int i = 0; i < 3; i++)
			{
				ImGui.TableNextColumn();
				ImGui.TableHeader(headerText[i]);

				// todo 最好能让拉伸的时候只拉伸第一列
				if (tableWidth.Length > i && tableWidth[i] >= 0)
					ImGui.TableSetupColumn(headerText[i], ImGuiTableColumnFlags.None, tableWidth[i]);
				else
					ImGui.TableSetupColumn(headerText[i]);
			}

			for (int i = 0; i < atkResNodeList.Length; i++)
			{
				resNode = atkResNodeList[i];
				iconId = ((AtkComponentIcon*)resNode->GetComponent())->IconId;

				if (itemArray[i] == null) Plugin.plugin.ChatGui.Print(i + "为null");
				//放入交易格子中再取消时，iconId会残留为之前的数值
				if (!resNode->IsVisible || iconId < 1)
				{
					itemArray[i].name = "";
					itemArray[i].count = 0;
					itemArray[i].price = 0;
					ImGui.TableNextColumn();
					ImGui.Text("");
					ImGui.TableNextColumn();
					ImGui.Text("");
					ImGui.TableNextColumn();
					ImGui.Text("");
					continue;
				}
				iconIdStr = Convert.ToString(iconId);
				ImGui.TableNextColumn();

				if (iconIdStr.StartsWith("10"))//HQ物品
					iconId = Convert.ToInt64(iconIdStr[2..]);

				var item1 = Plugin.plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Icon == iconId);
				int priceType = 0;//0-默认， 1-设定HQ交易NQ， 2-设定NQ交易HQ
				if (item1 != null)
				{
					if (iconIdStr.StartsWith("10"))
						itemName = item1.Name + "HQ";
					else
						itemName = item1.Name;

					itemArray[i].name = itemName;
					ImGui.Text(itemName);

					if (Plugin.plugin.Configuration.preset.ContainsKey(itemName))
					{
						itemArray[i].price = Plugin.plugin.Configuration.preset[itemName];
					}
					else
					{
						if (!itemName.EndsWith("HQ"))
						{
							if (Plugin.plugin.Configuration.preset.ContainsKey(itemName+"HQ"))
							{
								itemArray[i].price = Plugin.plugin.Configuration.preset[itemName + "HQ"];
								priceType = 1;
							}
						}
						else {
							if (Plugin.plugin.Configuration.preset.ContainsKey(itemName[0..^2]))
							{
								itemArray[i].price = Plugin.plugin.Configuration.preset[itemName[0..^2]];
								priceType = 2;
							}
						}
					}
				}
				else
				{
					itemArray[i].name = "获取失败";
					itemArray[i].price = 0;
					ImGui.Text("获取失败");
				}

				countStr = resNode->GetAsAtkComponentNode()->Component->UldManager.NodeList[6]->GetAsAtkTextNode()->NodeText.ToString();
				if (countStr == null || countStr.Length == 0)
					count = 1;
				else
					count = Convert.ToInt32(countStr);

				itemArray[i].count = count;
				ImGui.TableNextColumn();
				ImGui.Text(Convert.ToString(count));

				ImGui.TableNextColumn();
				if (itemArray[i].price == 0)
					ImGui.TextColored(color[priceType], "---");
				else
					ImGui.TextColored(color[priceType], String.Format("{0:0,0}", itemArray[i].price).TrimStart('0') + "/" + String.Format("{0:0,0}", itemArray[i].price * itemArray[i].count).TrimStart('0'));
			}

			ImGui.TableNextColumn();
			ImGui.Text("金币");

			ImGui.TableNextColumn();
			ImGui.Text(gilTextNode->NodeText.ToString());
			gil = Convert.ToInt32(gilTextNode->NodeText.ToString().Replace(",", ""));

			int sum = 0;
			ImGui.TableNextColumn();
			for (int i = 0; i < 5; i++)
			{
				if (itemArray[i].price != 0) sum += itemArray[i].count * itemArray[i].price;
			}
			sum += gil;
			ImGui.Text(String.Format("{0:0,0}", sum).TrimStart('0'));

			ImGui.EndTable();
		}


	}
}
