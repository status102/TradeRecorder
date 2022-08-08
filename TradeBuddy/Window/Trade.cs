using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using TradeBuddy.Model;

namespace TradeBuddy
{
	public class Trade : IDisposable
	{
		private readonly static Vector4[] color = new Vector4[] { new(1, 1, 1, 1), new(0, 1, 0, 1), new(1, 1, 0, 1) };
		/// <summary>
		/// 主窗口大小
		/// </summary>
		private const int Width = 540, Height = 560;
		private readonly static string[] Header_Title = { "", "物品", "数量", "预期金额", "最低价" };
		private readonly static float[] Col_Width = { 26, -1, 120, 150, 120 };
		private readonly static Vector2 Image_Size = new(26, 26);
		private const int Row_Height = 30;
		private  TextureWrap? Gil_Image => TradeBuddy.GetIcon(65002);



		public bool tradeOnceVisible = true;//保存单次交易时，监控窗口是否显示

		private readonly TradeBuddy TradeBuddy;
		private Configuration Config => TradeBuddy.Configuration;
		private PluginUI TradeUI => TradeBuddy.PluginUi;

		private byte[] byteBuffer = new byte[200];
		private TradeItem[] giveItem = new TradeItem[5] { new(), new(), new(), new(), new() };
		private TradeItem[] receiveItem = new TradeItem[5] { new(), new(), new(), new(), new() };

		private string tradeTarget = "";
		private int giveGil = 0, receiveGil = 0;
		
		//交易的物品和金币，在进入二次确认的时候暂存，防止记录时失败存0
		private int tradeGiveGil = 0, tradeReceiveGil = 0;
		private List<KeyValuePair<string, int>> tradeGiveItem = new(), tradeReceiveItem = new();

		private bool firstTrade = true;
		private string historyTarget = "";
		private int historyGiveGil = 0, historyReceiveGil = 0;
		private Dictionary<string, int> historyGiveMap = new(), historyReceiveMap = new();

		public unsafe void Draw(bool tradeVisible, ref bool twiceCheck, ref bool historyVisible, ref bool settingVisivle) {
			var tradeAddess = TradeBuddy.GameGui.GetAddonByName("Trade", 1);
			if (tradeAddess == IntPtr.Zero) {
				tradeOnceVisible = true;//交易窗口关闭后重置单次关闭设置
				twiceCheck = false;//二次确认时取消
				return;
			}
			if (!tradeOnceVisible || !tradeVisible)
				return;
			if (TradeUI.atkArrayDataHolder == null || TradeUI.atkArrayDataHolder->StringArrayCount < 10)
				return;

			var trade = (AtkUnitBase*)tradeAddess;
			if (trade->UldManager.LoadedState != 3 || trade->UldManager.NodeListCount <= 0)
				return;//等待交易窗口加载完毕


			ImGui.SetNextWindowSize(new Vector2(Width, Height), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(trade->X - Width, trade->Y), ImGuiCond.Appearing);
			if (ImGui.Begin("玩家交易", ref tradeOnceVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)) {

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
				if (ImGui.IsItemHovered()) {
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
				if (ImGuiComponents.IconButton(FontAwesomeIcon.History))
					historyVisible = true;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
					settingVisivle = true;

				try {
					DrowTradeTable(ref twiceCheck, ref giveGil, 0, giveResNode, giveMoney, ref giveItem);
				} catch (Exception e) {
					PluginLog.Error(e.ToString());
				}

				var receiveTarget = trade->UldManager.NodeList[20]->GetAsAtkTextNode()->NodeText;
				var receiveMoney = trade->UldManager.NodeList[6]->GetAsAtkTextNode();
				var receiveChecked = trade->UldManager.NodeList[31]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode()->AtkResNode.ScaleY == 1;

				ImGui.Spacing();
				tradeTarget = receiveTarget.ToString();

				if (receiveChecked)
					ImGuiComponents.TextWithLabel(tradeTarget + " -->", "  已确认");
				else
					ImGuiComponents.TextWithLabel(tradeTarget + " -->", "");
				try {
					DrowTradeTable(ref twiceCheck, ref receiveGil, 5, receiveResNode, receiveMoney, ref receiveItem);
				} catch (Exception e) {
					PluginLog.Error(e.ToString());
				}

				if (receiveChecked && giveChecked)//双方交易确认，进入最终确认状态
				{
					var selectYesno = TradeBuddy.GameGui.GetAddonByName("SelectYesno", 1);
					if (selectYesno != IntPtr.Zero && ((AtkUnitBase*)selectYesno)->UldManager.LoadedState == 3) {
						//var yesButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[11];
						//var noButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[8];
						if (!twiceCheck) {
							tradeGiveGil = giveGil;
							tradeGiveItem = new();
							giveItem.Where(item => item.count > 0).Select(item => new KeyValuePair<string, int>(item.name, item.count)).ToList().ForEach(i => tradeGiveItem.Add(new(i.Key, i.Value)));

							tradeReceiveGil = receiveGil;
							tradeReceiveItem = new();
							receiveItem.Where(item => item.count > 0).Select(item => new KeyValuePair<string, int>(item.name, item.count)).ToList().ForEach(i => tradeReceiveItem.Add(new(i.Key, i.Value)));

						}
						twiceCheck = true;
					}
				}
				ImGui.End();
			}
		}

		//绘制交易物品表格
		private unsafe void DrowTradeTable(ref bool twiceCheck, ref int gil, int offset, AtkResNode*[] atkResNodeList, AtkTextNode* gilTextNode, ref TradeItem[] itemArray) {
			ImGui.BeginTable("交易栏", Header_Title.Length, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);

			for (int i = 0; i < Header_Title.Length; i++) {
				if (Col_Width.Length > i) {
					if (Col_Width[i] >= 0)
						ImGui.TableSetupColumn(Header_Title[i], ImGuiTableColumnFlags.WidthFixed, Col_Width[i]);
					else
						ImGui.TableSetupColumn(Header_Title[i], ImGuiTableColumnFlags.WidthStretch);
				}
			}
			ImGui.TableHeadersRow();

			for (int i = 0; i < atkResNodeList.Length; i++) {
				ImGui.TableNextRow(ImGuiTableRowFlags.None, Row_Height);

				if (itemArray[i] == null) {
					itemArray[i] = new();
					continue;
				}

				AtkResNode* resNode = atkResNodeList[i];
				long iconId = ((AtkComponentIcon*)resNode->GetComponent())->IconId;

				//放入交易格子中再取消时，iconId会残留为之前的数值
				if (!resNode->IsVisible || iconId < 1) {
					itemArray[i] = new();
					continue;
				}
				if (!twiceCheck) {
					byte* bytePtr = TradeUI.atkArrayDataHolder->StringArrays[9]->StringArray[offset + i];

					Array.Fill<byte>(byteBuffer, 0);
					int len = 0;
					for (int index = 0; index < byteBuffer.Length && bytePtr[index] != 0; index++) {
						len = index + 1;
						byteBuffer[index] = bytePtr[index];
					}
					if (len <= 24) {
						itemArray[i] = new();
						continue;
					}

					byte[] strBuffer = new byte[len - 24];
					//前面抛弃14字节，后面抛弃10字节
					Array.Copy(byteBuffer, 14, strBuffer, 0, len - 24);

					var strName = Encoding.UTF8.GetString(strBuffer).Replace("", "HQ");
					if (itemArray[i].name != strName) {
						string iconIdStr = Convert.ToString(iconId);
						//iconId以10开头的为HQ
						if (itemArray[i].isHQ = iconIdStr.StartsWith("10"))
							itemArray[i].iconId = uint.Parse(iconIdStr[2..]);
						else
							itemArray[i].iconId = (uint)iconId;
						itemArray[i].icon = TradeBuddy.GetIcon(itemArray[i].iconId, itemArray[i].isHQ);

						itemArray[i].name = strName;
						itemArray[i].price = 0;
						itemArray[i].priceType = 0;
						itemArray[i].priceName = strName;

						if (Config.PresetItemDictionary.ContainsKey(itemArray[i].name)) { } else if (itemArray[i].isHQ && Config.PresetItemDictionary.ContainsKey(itemArray[i].name[0..^2])) {
							itemArray[i].priceType = 2;
							itemArray[i].priceName = itemArray[i].name[0..^2];
						} else if (Config.PresetItemDictionary.ContainsKey(itemArray[i].name + "HQ")) {
							itemArray[i].priceType = 1;
							itemArray[i].priceName = itemArray[i].name + "HQ";
						}
						itemArray[i].priceList = new();
						string priceName = itemArray[i].priceName;
						var search = Config.PresetItemList.Find(s => s.ItemName == priceName);
						if (search != null)
							itemArray[i].priceList = search.PriceList;

						itemArray[i].UpdateMinPrice();
					}
				}

				ImGui.TableNextColumn();
				var icon = itemArray[i].icon;
				if (icon != null)
					ImGui.Image(icon.ImGuiHandle, Image_Size);

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(itemArray[i].name);

				if (ImGui.IsItemHovered()) {
					try {
						var itemPresetStr = Config.PresetItemList[Config.PresetItemDictionary[itemArray[i].name]].GetPriceStr();
						if (!string.IsNullOrEmpty(itemPresetStr))
							ImGui.SetTooltip($"{itemArray[i].priceName} 预设：{itemPresetStr}");
					} catch (KeyNotFoundException) { }
				}

				string countStr = ((AtkComponentIcon*)resNode->GetComponent())->QuantityText->NodeText.ToString().Replace(",", "").Trim();
				int count;
				if (string.IsNullOrEmpty(countStr))
					count = 1;
				else
					count = Convert.ToInt32("0" + countStr.Replace(",", string.Empty));
				//如果数量刷新则重置计算价格
				if (itemArray[i].count != count) {
					itemArray[i].count = count;
					itemArray[i].price = 0;
				}
				ImGui.TableNextColumn();
				ImGui.TextUnformatted(Convert.ToString(count));

				ImGui.TableNextColumn();
				if (itemArray[i].priceList.Count == 0) {
					ImGui.TextColored(color[itemArray[i].priceType], "---");
					itemArray[i].price = 0;
				} else {
					if (itemArray[i].price == 0) {
							var countList = itemArray[i].priceList.Keys.ToList();
							countList.Sort(PresetItem.Sort);
							foreach (int num in countList) {
								if (itemArray[i].count / num * num == itemArray[i].count) {
									itemArray[i].price = itemArray[i].count / num * itemArray[i].priceList[num];
									break;
								}
							}
					}
					ImGui.TextColored(color[itemArray[i].priceType], $"{itemArray[i].price:#,0}");
					if (ImGui.IsItemClicked())
						ImGui.SetClipboardText($"{itemArray[i].price:#,0}");
				}

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(itemArray[i].GetMinPriceStr());
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"{itemArray[i].GetMinPriceStr()}<{itemArray[i].minPriceServer}>");
			}

			ImGui.TableNextRow(ImGuiTableRowFlags.None, Row_Height);
			ImGui.TableNextColumn();

			if (Gil_Image != null)
				ImGui.Image(Gil_Image.ImGuiHandle, Image_Size);

			ImGui.TableNextColumn();
			ImGui.TextUnformatted($"{gilTextNode->NodeText}");

			gil = Convert.ToInt32("0" + gilTextNode->NodeText.ToString().Replace(",", String.Empty).Trim());

			float sum = 0;
			ImGui.TableNextColumn();
			foreach (var item in itemArray) 
				sum += item.price;

			sum += gil;
			ImGui.TextUnformatted($"{sum:#,0}");
			if (ImGui.IsItemHovered())
				ImGui.SetTooltip("包含金币在内的全部金额，单击复制");
			if (ImGui.IsItemClicked())
				ImGui.SetClipboardText($"{sum:#,0}");

			ImGui.TableNextColumn();
			int min = 0;
			ImGui.TableNextColumn();
			foreach (var item in itemArray) {
				if (item.minPrice > 0)
					min += item.minPrice * item.count;
			}
			ImGui.TextUnformatted($"{min:#,0}");

			ImGui.EndTable();
		}

		public void MessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
			if (TradeUI.twiceCheck && type == XivChatType.SystemMessage) {
				//Type：SystemMessage；sid：0；sender：；msg：交易完成。；isHand：False
				//Type：SystemMessage；sid：0；sender：；msg：交易取消。；isHand：False

				if (message.TextValue == Config.TradeConfirmStr) {
					TradeUI.History.AddHistory(tradeTarget, tradeGiveGil, tradeReceiveGil, tradeGiveItem, tradeReceiveItem);
					//初次交易目标或者切换交易目标后清空交易历史记录
					if (string.IsNullOrEmpty(historyTarget) || historyTarget != tradeTarget) {
						firstTrade = true;
						historyTarget = tradeTarget;

						historyGiveGil = 0;
						historyReceiveGil = 0;
						historyGiveMap = new();
						historyReceiveMap = new();
					}

					historyGiveGil += tradeGiveGil;
					historyReceiveGil += tradeReceiveGil;
					tradeGiveItem.ForEach(pair =>
					{
						if (historyGiveMap.ContainsKey(pair.Key))
							historyGiveMap[pair.Key] += pair.Value;
						else
							historyGiveMap[pair.Key] = pair.Value;
					});
					tradeReceiveItem.ForEach(pair =>
					{
						if (historyReceiveMap.ContainsKey(pair.Key))
							historyReceiveMap[pair.Key] += pair.Value;
						else
							historyReceiveMap[pair.Key] = pair.Value;
					});

					if (Config.TradeConfirmAlert) {
						var giveList = tradeGiveItem.Select(kp => $"{kp.Key}x{kp.Value}").ToList();
						var receiveList = tradeReceiveItem.Select(kp => $"{kp.Key}x{kp.Value}").ToList();
						giveList.Insert(0, $"{tradeGiveGil:#,0}G");
						receiveList.Insert(0, $"{tradeReceiveGil:#,0}G");


						var historyGiveList = historyGiveMap.Select(kp => $"{kp.Key}x{kp.Value}").ToList();
						var historyReceiveList = historyReceiveMap.Select(kp => $"{kp.Key}x{kp.Value}").ToList();
						historyGiveList.Insert(0, $"{historyGiveGil:#,0}G");
						historyReceiveList.Insert(0, $"{historyReceiveGil:#,0}G");

						var tradeStr = string.Format("[{0:}]交易成功: ==>{1:}\n<<==   {2:}\n==>>   {3:}",
							TradeBuddy.Name, tradeTarget,
							string.Join(", ", giveList), string.Join(", ", receiveList));
						var historyStr = string.Format("\n连续交易累积：\n<<==   {0:}\n==>>   {1:}",
							string.Join(", ", historyGiveList), string.Join(", ", historyReceiveList));

						TradeBuddy.ChatGui.Print(firstTrade ? tradeStr : (tradeStr + historyStr));
					}
					firstTrade = false;
				} else if (message.TextValue == Config.TradeCancelStr) {
					TradeUI.History.AddHistory(false, tradeTarget, tradeGiveGil, tradeReceiveGil, tradeGiveItem, tradeReceiveItem);

					var giveList = tradeGiveItem.Select(kp => $"{kp.Key}x{kp.Value}").ToList();
					var receiveList = tradeReceiveItem.Select(kp => $"{kp.Key}x{kp.Value}").ToList();
					giveList.Insert(0, $"{tradeGiveGil:#,0}G");
					receiveList.Insert(0, $"{tradeReceiveGil:#,0}G");
					if (Config.TradeCancelAlert) {
						TradeBuddy.ChatGui.Print(string.Format("[{0:}]交易取消: ==>{1:}\n<<==   {2:}\n==>>   {3:}",
							TradeBuddy.Name, tradeTarget,
							string.Join(", ", giveList), string.Join(", ", receiveList)));
					}
				}
			}
		}

		public Trade(TradeBuddy tradeBuddy) {
			TradeBuddy = tradeBuddy;
			TradeBuddy.ChatGui.ChatMessage += MessageDelegate;
		}
		public void Dispose() {
			TradeBuddy.ChatGui.ChatMessage -= MessageDelegate;
		}
	}
}
