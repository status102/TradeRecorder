using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
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

namespace TradeBuddy
{
	public class Trade
	{
		public class Item
		{
			private uint _iconId = 0;
			public uint iconId
			{
				get => _iconId; set
				{
					_iconId = value;
					icon = Configuration.GetIcon(iconId, isHQ);
				}
			}
			public TextureWrap? icon { get; private set; }
			public bool isHQ = false;
			public string name = "";
			public int count = 0;
			public int price = 0;
			/// <summary>
			/// 0-默认， 1-设定HQ交易NQ， 2-设定NQ交易HQ
			/// </summary>
			public byte priceType = 0;
			/// <summary>
			/// 所匹配的预设名称
			/// </summary>
			public string priceName = "";
			/// <summary>
			/// 联网获取最低价，-2未初始化，-1获取失败，0获取中
			/// </summary>
			public int minPrice { get; private set; } = -2;
			public string minPriceServer { get; private set; } = "";

			public string minPriceStr
			{
				get
				{
					if (minPrice == -2)
					{
						minPrice = 0;
						uint itemId = 0;
						string worldName = Configuration.GetWorldName();
						var itemByName = DalamudDll.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == (isHQ ? name[0..^2] : name));
						if (itemByName != null)
							itemId = itemByName.RowId;
						//todo 获取Universalis价格
						if (itemId > 1 && !string.IsNullOrEmpty(worldName))
							Universalis.Client
								.GetCurrentlyShownView(worldName, itemId, price =>
								{
									if (price == null)
										minPrice = -1;
									else
									{
										minPrice = isHQ ? price.minPriceHQ : price.minPriceNQ;
										minPriceServer = price.listings?[0].worldName ?? "";
									}
								});
						else
							minPrice = -1;
					}
					return minPrice switch
					{
						-1 => "获取失败",
						0 => "获取中",
						_ => $"{minPrice:#,##0}"
					};
				}
			}

			public Dictionary<int, int> priceList = new();
		}
		private readonly static Vector4[] color = new Vector4[] { new(1, 1, 1, 1), new(0, 1, 0, 1), new(1, 1, 0, 1) };
		/// <summary>
		/// 主窗口大小
		/// </summary>
		private const int Width = 540, Height = 480;
		private readonly static string[] Header_Title = new string[] { "", "物品", "数量", "预期金额", "最低价" };
		private readonly static float[] Col_Width = new float[] { 20, -1, 120, 150, 120 };
		private const int Row_Height = 20;
		private readonly static Vector2 Image_Size = new(20, 20);
		private readonly TextureWrap? Gil_Image = Configuration.GetIcon(65002, false);
		private TradeBuddy _tradeBuddy;
		private Configuration Config => _tradeBuddy.Configuration;
		private PluginUI TradeUI => _tradeBuddy.PluginUi;

		private byte[] byteBuffer = new byte[200];
		private Item[] giveItem = new Item[5] { new(), new(), new(), new(), new() };
		private Item[] receiveItem = new Item[5] { new(), new(), new(), new(), new() };

		private string tradeTarget = "";
		private int giveGil = 0, receiveGil = 0;
		/// <summary>
		/// 交易的物品，确认交易时存入
		/// </summary>
		private List<KeyValuePair<string, int>> giveItemList = new(), receiveItemList = new();

		private string historyTarget = "";
		private int historyGiveGil = 0, historyReceiveGil = 0;
		private Dictionary<string, int> historyGiveList = new(), historyReceiveList = new();

		public Trade(TradeBuddy tradeBuddy)
		{
			_tradeBuddy = tradeBuddy;
		}
		public unsafe void DrawTrade(bool tradeVisible, ref bool _tradeOnceVisible, ref bool twiceCheck, ref bool historyVisible, ref bool settingVisivle)
		{
			var tradeAddess = DalamudDll.GameGui.GetAddonByName("Trade", 1);
			if (tradeAddess == IntPtr.Zero)
			{
				_tradeOnceVisible = true;//交易窗口关闭后重置单次关闭设置
				twiceCheck = false;//二次确认时取消
				return;
			}
			if (!_tradeOnceVisible || !tradeVisible) return;
			if (TradeUI.atkArrayDataHolder == null || TradeUI.atkArrayDataHolder->StringArrayCount < 10) return;

			var trade = (AtkUnitBase*)tradeAddess;
			if (trade->UldManager.LoadedState != 3 || trade->UldManager.NodeListCount <= 0) return;//等待交易窗口加载完毕


			ImGui.SetNextWindowSize(new Vector2(Width, Height), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(trade->X - Width, trade->Y), ImGuiCond.Appearing);
			if (ImGui.Begin("玩家交易", ref _tradeOnceVisible, ImGuiWindowFlags.NoCollapse))
			{
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
					DrowTradeTable(ref twiceCheck, ref giveGil, 0, giveResNode, giveMoney, ref giveItem);
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}

				ImGui.Spacing();
				tradeTarget = receiveTarget.ToString();

				if (receiveChecked)
					ImGuiComponents.TextWithLabel(tradeTarget + " -->", "  已确认");
				else
					ImGuiComponents.TextWithLabel(tradeTarget + " -->", "");
				try
				{
					DrowTradeTable(ref twiceCheck, ref receiveGil, 5, receiveResNode, receiveMoney, ref receiveItem);
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}

				if (receiveChecked && giveChecked)//双方交易确认，进入最终确认状态
				{
					var selectYesno = DalamudDll.GameGui.GetAddonByName("SelectYesno", 1);
					if (selectYesno != IntPtr.Zero && ((AtkUnitBase*)selectYesno)->UldManager.LoadedState == 3)
					{
						//var yesButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[11];
						//var noButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[8];
						if (!twiceCheck)
						{
							giveItemList = new();
							giveItem.Where(item => item.count > 0).Select(item => new KeyValuePair<string, int>(item.name, item.count)).ToList().ForEach(i => giveItemList.Add(new(i.Key, i.Value)));

							receiveItemList = new();
							receiveItem.Where(item => item.count > 0).Select(item => new KeyValuePair<string, int>(item.name, item.count)).ToList().ForEach(i => receiveItemList.Add(new(i.Key, i.Value)));

						}
						twiceCheck = true;
					}
				}
			}
			ImGui.End();
		}

		//绘制交易物品表格
		private unsafe void DrowTradeTable(ref bool twiceCheck, ref int gil, int offset, AtkResNode*[] atkResNodeList, AtkTextNode* gilTextNode, ref Item[] itemArray)
		{
			ImGui.BeginTable("交易栏", Header_Title.Length, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);

			for (int i = 0; i < Header_Title.Length; i++)
			{
				if (Col_Width.Length > i)
				{
					if (Col_Width[i] >= 0)
						ImGui.TableSetupColumn(Header_Title[i], ImGuiTableColumnFlags.WidthFixed, Col_Width[i]);
					else
						ImGui.TableSetupColumn(Header_Title[i], ImGuiTableColumnFlags.WidthStretch);
				}
			}
			ImGui.TableHeadersRow();

			for (int i = 0; i < atkResNodeList.Length; i++)
			{
				ImGui.TableNextRow(ImGuiTableRowFlags.None, Row_Height);

				if (itemArray[i] == null)
				{
					itemArray[i] = new();
					continue;
				}

				AtkResNode* resNode = atkResNodeList[i];
				if (!twiceCheck)
				{
					long iconId = ((AtkComponentIcon*)resNode->GetComponent())->IconId;

					//放入交易格子中再取消时，iconId会残留为之前的数值
					if (!resNode->IsVisible || iconId < 1)
					{
						itemArray[i] = new();
						continue;
					}

					byte* bytePtr = TradeUI.atkArrayDataHolder->StringArrays[9]->StringArray[offset + i];

					Array.Fill<byte>(byteBuffer, 0);
					int len = 0;
					for (int index = 0; index < byteBuffer.Length && bytePtr[index] != 0; index++)
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

					var strName = Encoding.UTF8.GetString(strBuffer).Replace("", "HQ");
					if (itemArray[i].name != strName)
					{
						string iconIdStr = Convert.ToString(iconId);
						//iconId以10开头的为HQ
						if (itemArray[i].isHQ = iconIdStr.StartsWith("10"))
							itemArray[i].iconId = uint.Parse(iconIdStr[2..]);
						else
							itemArray[i].iconId = (uint)iconId;

						itemArray[i].name = strName;
						itemArray[i].price = 0;
						itemArray[i].priceType = 0;
						itemArray[i].priceName = strName;

						if (Config.PresetItemDictionary.ContainsKey(itemArray[i].name))
						{ }
						else if (itemArray[i].isHQ && Config.PresetItemDictionary.ContainsKey(itemArray[i].name[0..^2]))
						{
							itemArray[i].priceType = 2;
							itemArray[i].priceName = itemArray[i].name[0..^2];
						}
						else if (Config.PresetItemDictionary.ContainsKey(itemArray[i].name + "HQ"))
						{
							itemArray[i].priceType = 1;
							itemArray[i].priceName = itemArray[i].name + "HQ";
						}
						//todo 增加联网价格获取
						itemArray[i].priceList = new();
						string priceName = itemArray[i].priceName;
						var search = Config.PresetItemList.Find(s => s.ItemName == priceName);
						if (search != null) itemArray[i].priceList = search.PriceList;

					}
				}
				ImGui.TableNextColumn();
				var icon = itemArray[i].icon;
				if (icon != null) ImGui.Image(icon.ImGuiHandle, Image_Size);

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(itemArray[i].name);
				if (ImGui.IsItemHovered())
				{
					var itemPresetStr = Config.PresetItemList[Config.PresetItemDictionary[itemArray[i].name]].GetPriceStr();
					if (!string.IsNullOrEmpty(itemPresetStr)) ImGui.SetTooltip($"{itemArray[i].priceName} 预设：{itemPresetStr}");
				}

				string countStr = ((AtkComponentIcon*)resNode->GetComponent())->QuantityText->NodeText.ToString().Replace(",", "").Trim();
				int count;
				if (string.IsNullOrEmpty(countStr))
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
				ImGui.TextUnformatted(Convert.ToString(count));

				ImGui.TableNextColumn();
				if (itemArray[i].priceList.Count == 0)
				{
					ImGui.TextColored(color[itemArray[i].priceType], "---");
					itemArray[i].price = 0;
				}
				else
				{
					if (itemArray[i].price == 0)
					{
						if (Config.StrictMode)
						{
							var countList = itemArray[i].priceList.Keys.ToList();
							countList.Sort(Configuration.PresetItem.Sort);
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
					ImGui.TextColored(color[itemArray[i].priceType], $"{itemArray[i].price:#,##0}");
					if (ImGui.IsItemClicked()) ImGui.SetClipboardText($"{itemArray[i].price:#,##0}");
				}

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(itemArray[i].minPriceStr);
				if (ImGui.IsItemHovered()) ImGui.SetTooltip($"{itemArray[i].minPriceStr}<{itemArray[i].minPriceServer}>");
			}

			ImGui.TableNextRow(ImGuiTableRowFlags.None, Row_Height);
			ImGui.TableNextColumn();

			if (Gil_Image != null) ImGui.Image(Gil_Image.ImGuiHandle, Image_Size);

			ImGui.TableNextColumn();
			ImGui.TextUnformatted("金币");

			ImGui.TableNextColumn();
			ImGui.TextUnformatted(gilTextNode->NodeText.ToString());

			gil = Convert.ToInt32("0" + gilTextNode->NodeText.ToString().Replace(",", String.Empty).Trim());

			float sum = 0;
			ImGui.TableNextColumn();
			for (int i = 0; i < 5; i++)
				sum += itemArray[i].price;

			sum += gil;
			ImGui.TextUnformatted($"{sum:#,##0}");
			if (ImGui.IsItemHovered()) ImGui.SetTooltip("包含金币在内的全部金额");
			if (ImGui.IsItemClicked()) ImGui.SetClipboardText($"{sum:#,##0}");
			ImGui.EndTable();
		}

		public void MessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (TradeUI.twiceCheck && type == XivChatType.SystemMessage)
			{
				//Type：SystemMessage；sid：0；sender：；msg：交易完成。；isHand：False
				//Type：SystemMessage；sid：0；sender：；msg：交易取消。；isHand：False
#if DEBUG
				DalamudDll.ChatGui.Print("=== Give ===");
				giveItemList.ForEach(i => DalamudDll.ChatGui.Print($"{i.Key}-{i.Value}"));
				DalamudDll.ChatGui.Print("=== Receive ===");
				receiveItemList.ForEach(i => DalamudDll.ChatGui.Print($"{i.Key}-{i.Value}"));
#endif
				if (message.TextValue == Config.TradeConfirmStr)
				{
					TradeUI.History.PushTradeHistory(tradeTarget, giveGil, receiveGil, giveItemList, receiveItemList);
					if (string.IsNullOrEmpty(historyTarget) || historyTarget != tradeTarget)
					{
						historyTarget = tradeTarget;

						historyGiveGil = 0;
						historyReceiveGil = 0;
						historyGiveList = new();
						historyReceiveList = new();
					}

					historyGiveGil += giveGil;
					historyReceiveGil += receiveGil;
					giveItemList.ForEach(pair =>
					{
						if (historyGiveList.ContainsKey(pair.Key))
							historyGiveList[pair.Key] += pair.Value;
						else
							historyGiveList[pair.Key] = pair.Value;
					});
					receiveItemList.ForEach(pair =>
					{
						if (historyReceiveList.ContainsKey(pair.Key))
							historyReceiveList[pair.Key] += pair.Value;
						else
							historyReceiveList[pair.Key] = pair.Value;
					});

					if (Config.TradeConfirmAlert)
					{
						DalamudDll.ChatGui.Print(string.Format("[{0:}]交易成功: ==>{1:}\n<<==   {2:}G, {3:}\n==>>   {4:}G, {5:}\n连续交易累积：\n<<==   {6:}G, {7:}\n==>>   {8:}G, {9:}",
							_tradeBuddy.Name, tradeTarget,
							giveGil, string.Join(',', giveItemList.Select(kp => $"{kp.Key}x{kp.Value}")),
							receiveGil, string.Join(',', receiveItemList.Select(kp => $"{kp.Key}x{kp.Value}")),
							historyGiveGil, string.Join(',', historyGiveList.Select(kp => $"{kp.Key}x{kp.Value}")),
							historyReceiveGil, string.Join(',', historyReceiveList.Select(kp => $"{kp.Key}x{kp.Value}"))));
					}
				}
				else if (message.TextValue == Config.TradeCancelStr)
				{
					TradeUI.History.PushTradeHistory(false, tradeTarget, giveGil, receiveGil, giveItemList, receiveItemList);

					if (Config.TradeCancelAlert)
					{
						DalamudDll.ChatGui.Print(string.Format("[{0:}]交易取消: ==>{1:}\n<<==   {2:}G, {3:}\n==>>   {4:}G, {5:}",
							_tradeBuddy.Name, tradeTarget,
							giveGil, string.Join(',', giveItemList.Select(kp => $"{kp.Key}x{kp.Value}")),
							receiveGil, string.Join(',', receiveItemList.Select(kp => $"{kp.Key}x{kp.Value}"))));
					}
				}
			}
		}

	}
}
