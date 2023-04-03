using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TradeBuddy.Model;

namespace TradeBuddy.Window
{
	public class Trade2
	{
		private TradeBuddy tradeBuddy { get; init; }
		/// <summary>
		/// 主窗口大小
		/// </summary>
		private const int WIDTH = 540, HEIGHT = 560;
		/// <summary>
		/// 显示价格的颜色，RBGA
		/// </summary>
		private readonly static Vector4[] COLOR = new Vector4[] { new(1, 1, 1, 1), new(0, 1, 0, 1), new(1, 1, 0, 1) };
		private readonly static string[] COL_NAME = { "", "物品", "数量", "预期", "最低价" };
		private readonly static float[] COL_WIDTH = { 26, -1, 42, 80, 80 };
		private const int ROW_HEIGHT = 30;
		private readonly static Vector2 IMAGE_SIZE = new(26, 26);
		private TextureWrap? GIL_IMAGE => PluginUI.GetIcon(65002);


		/// <summary>
		/// 是否交易中
		/// </summary>
		private bool trading = false;
		/// <summary>
		/// 交易成功
		/// </summary>
		private bool success = false;
		/// <summary>
		/// 交易物品记录，0自己，1对面；[,0]id，[,1]数量，[,2]是否为HQ
		/// </summary>
		private TradeItem[][] tradeItemList = new TradeItem[2][];
		private struct TradeItem
		{
			public uint Id { get; init; } = 0;
			public ushort? Icon { get; init; } = 0;
			public uint Count { get; set; } = 0;
			public string? Name { get; init; } = null;
			public bool Quality { get; init; } = false;
			public uint StackSize { get; init; } = 0;
			public TradeItem() { }
		}
		/// <summary>
		/// 交易金币记录，0自己，1对面
		/// </summary>
		private uint[] tradeGil = new uint[2];
		/// <summary>
		/// 连续交易物品记录，Key itemId，Value (name, nq, hq, stackSize)
		/// </summary>
		private Dictionary<uint, RecordItem>[] multiItemList = new Dictionary<uint, RecordItem>[2] { new(), new() };
		private struct RecordItem
		{
			public uint Id { get; init; }
			public string Name { get; init; }
			public uint NqCount { get; set; }
			public uint HqCount { get; set; }
			public uint StackSize { get; init; }
		}
		private uint[] multiGil = new uint[2] { 0, 0 };
		private bool onceVisible = true;
		private int[] position = new int[2];
		private string target = "";
		private string lastTarget = "";
		/// <summary>
		/// 对方交易栏的发包序号，序号步进后需要清空道具栏
		/// </summary>
		private ushort targetRound = 0;
		private Configuration config => tradeBuddy.Configuration;
		#region Init
		public Trade2(TradeBuddy tradeBuddy) {
			this.tradeBuddy = tradeBuddy;
			tradeBuddy.GameNetwork.NetworkMessage += NetworkMessageDelegate;
		}
		public void Dispose() {
			tradeBuddy.GameNetwork.NetworkMessage -= NetworkMessageDelegate;
		}
		#endregion
		public unsafe void Draw() {
			if (!config.ShowTrade || !trading || !onceVisible)
				return;
			if (position[0] == int.MinValue) {
				var address = tradeBuddy.GameGui.GetAddonByName("Trade", 1);
				if (address != IntPtr.Zero && ((AtkUnitBase*)address)->UldManager.LoadedState == AtkLoadState.Loaded) {
					position[0] = ((AtkUnitBase*)address)->X - WIDTH - 5;
					position[1] = ((AtkUnitBase*)address)->Y + 2;
				}
			}
			ImGui.SetNextWindowSize(new Vector2(WIDTH, HEIGHT), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(position[0], position[1]), ImGuiCond.Appearing);
			if (ImGui.Begin("玩家交易2", ref onceVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)) {


				ImGui.TextUnformatted("<--");

				ImGui.SameLine(ImGui.GetColumnWidth() - 90);
				ImGui.TextDisabled("(?)");
				if (ImGui.IsItemHovered()) {
					ImGui.BeginTooltip();
					ImGui.TextUnformatted("预期金额：");
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(COLOR[0], "左键单击复制到剪贴板");

					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(COLOR[0], "“---”为未设定预期金额");

					//绿色为设定HQ但交易NQ
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(COLOR[1], "设定了HQ的预期金额，但交易的是NQ物品");
					//黄色为设定NQ但交易HQ
					ImGui.Bullet();
					ImGui.SameLine();
					ImGui.TextColored(COLOR[2], "设定了NQ的预期金额，但交易的是HQ物品");

					ImGui.EndTooltip();
				}

				ImGui.AlignTextToFramePadding();
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.History))
					tradeBuddy.PluginUi.History.ShowHistory(target);

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
					tradeBuddy.PluginUi.Setting.Show();


				DrawTradeTable(tradeItemList[0], tradeGil[0]);
				ImGui.Spacing();
				ImGui.TextUnformatted(target + " -->");
				DrawTradeTable(tradeItemList[1], tradeGil[1]);

				ImGui.End();
			}
		}
		/// <summary>
		/// 绘制交易道具表
		/// </summary>
		/// <param name="itemList"></param>
		/// <param name="gil"></param>
		private void DrawTradeTable(TradeItem[] itemList, uint gil) {
			ImGui.BeginTable("交易栏", COL_NAME.Length, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);

			for (int i = 0; i < COL_NAME.Length; i++) {
				if (COL_WIDTH.Length > i) {
					if (COL_WIDTH[i] >= 0)
						ImGui.TableSetupColumn(COL_NAME[i], ImGuiTableColumnFlags.WidthFixed, COL_WIDTH[i]);
					else
						ImGui.TableSetupColumn(COL_NAME[i], ImGuiTableColumnFlags.WidthStretch);
				}
			}
			ImGui.TableHeadersRow();
			for (int i = 0; i < itemList.Length; i++) {
				ImGui.TableNextRow(ImGuiTableRowFlags.None, ROW_HEIGHT);

				ImGui.TableNextColumn();

				var icon = PluginUI.GetIcon(itemList[i].Icon ?? 0, itemList[i].Quality);
				if (icon != null)
					ImGui.Image(icon.ImGuiHandle, IMAGE_SIZE);

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(itemList[i].Name);

				// TODO 右键
				if (ImGui.IsItemHovered()) {
					try {
						var itemPresetStr = config.PresetItemList[config.PresetItemDictionary[itemList[i].Name ?? ""]].GetPriceStr();
						//if (!string.IsNullOrEmpty(itemPresetStr))
						//ImGui.SetTooltip($"{itemList[i].priceName} 预设：{itemPresetStr}");
					} catch (KeyNotFoundException) { }
				}

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(Convert.ToString(itemList[i].Count));

				ImGui.TableNextColumn();
				// TODO 绘制交易栏的预期金额
				/*
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
				}*/

				ImGui.TableNextColumn();
				/*
				ImGui.TextUnformatted(itemArray[i].GetMinPriceStr());
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"{itemArray[i].GetMinPriceStr()}<{itemArray[i].minPriceServer}>");
			*/
			}

			ImGui.TableNextRow(ImGuiTableRowFlags.None, ROW_HEIGHT);
			ImGui.TableNextColumn();

			if (GIL_IMAGE != null)
				ImGui.Image(GIL_IMAGE.ImGuiHandle, IMAGE_SIZE);

			ImGui.TableNextColumn();
			ImGui.TextUnformatted($"{gil:#,0}");

			float sum = 0;
			ImGui.TableNextColumn();
			ImGui.TableNextColumn();
			//foreach (var item in itemArray)
			//sum += item.price;

			sum += gil;
			ImGui.TextUnformatted($"{sum:#,0}");
			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"包含金币在内的全部金额，单击复制：{sum:#,0}");
			if (ImGui.IsItemClicked())
				ImGui.SetClipboardText($"{sum:#,0}");

			int min = 0;
			ImGui.TableNextColumn();
			/*
			foreach (var item in itemArray) {
				if (item.minPrice > 0)
					min += item.minPrice * item.count;
			}*/
			ImGui.TextUnformatted($"{min:#,0}");
			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"以最低价计算的金额，单击复制：{min:#,0}");
			if (ImGui.IsItemClicked())
				ImGui.SetClipboardText($"{min:#,0}");

			ImGui.EndTable();
		}

		/// <summary>
		/// 处理自身交易栏的变化，包括水晶、金币
		/// </summary>
		/// <param name="bytes"></param>
		private unsafe void UpdateMyTradeSlot(InventoryModifyHandler modify) {
			if (modify.unknown_03 == 0x10 && modify.unknown_05 == 0x03) {
				var type = modify.unknown_04;
				if (type == 0x02) {
					// 道具移动
					if (modify.Page >= 0 && modify.Page <= 3) {
						// 从背包出
						var item = Utils.GetInventoryItem(modify.Page, (int)modify.Slot);
						if (item == null) {
							PluginLog.Warning($"item为空[{modify.Page},{modify.Slot}]");
						} else {
							Task.Run(() => {
								var itemFromList = GetItemFromList(item->ItemID);
								tradeItemList[0][modify.Slot2] = new TradeItem() {
									Id = item->ItemID,
									Icon = itemFromList?.Icon,
									Count = 1, // item->Quantity,
									Quality = Convert.ToBoolean((uint)item->Flags & (uint)FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HQ),
									Name = itemFromList?.Name.RawString,
									StackSize = itemFromList?.StackSize ?? 0
								};
							});
						}
					} else if (modify.Page == 0x07D5 && modify.Page2 == 0x07D5) {
						// 交易栏内互相移动
						TradeItem a = tradeItemList[0][modify.Slot];
						tradeItemList[0][modify.Slot] = tradeItemList[0][modify.Slot2];
						tradeItemList[0][modify.Slot2] = a;
					} else if (modify.Page == 0x07D5) {
						// 删除某一栏道具
						tradeItemList[0][modify.Slot] = new TradeItem() { };
					} else if (modify.Page == 0x07D1) {
						// 水晶
						Task.Run(() => {
							var item = GetItemFromList((uint)modify.Slot + 2);
							tradeItemList[0][modify.Slot2] = new TradeItem() {
								Id = (uint)(modify.Slot + 2),
								Icon = item?.Icon,
								Count = modify.Count,
								Quality = false,
								Name = item?.Name.RawString,
								StackSize = item?.StackSize ?? 0
							};
						});
					}
				} else if (type == 0x1A) {
					// 变更某个格子的道具个数
					tradeItemList[0][modify.Slot].Count = modify.Count;
				} else if (type == 0x19) {
					// 金币
					tradeGil[0] = modify.Count;
				}
			}
		}
		/// <summary>
		/// 处理对方交易栏的物品变化
		/// </summary>
		/// <param name="bytes"></param>
		private void UpdateOtherTradeItem(byte[] bytes) {
			// TODO 数据包判断拆离当前逻辑
			if (bytes[3] == 0x10 && bytes[4] == 0xD9 && bytes[5] == 0x07 && bytes[8] == 0xD9 && bytes[9] == 0x07) {
				CheckOtherTradeRound(BitConverter.ToUInt16(bytes));

				var slot = BitConverter.ToUInt16(bytes, 0x0A);
				var count = BitConverter.ToUInt32(bytes, 0x0C);
				var itemId = BitConverter.ToUInt16(bytes, 0x10);
				var isHq = bytes[0x20];

				Task.Run(() => {
					var item = GetItemFromList(itemId);
					tradeItemList[1][slot] = new TradeItem() {
						Id = itemId,
						Icon = item?.Icon,
						Count = count,
						Quality = Convert.ToBoolean(isHq),
						Name = item?.Name.RawString,
						StackSize = item?.StackSize ?? 0
					};
				});
			}
		}
		/// <summary>
		/// 处理对方交易栏的水晶、金币变化
		/// </summary>
		/// <param name="bytes"></param>
		private void UpdateOtherTradeMoney(byte[] bytes) {
			// TODO 数据包判断拆离当前逻辑
			if (bytes[3] == 0x10 && bytes[4] == 0xD9 && bytes[5] == 0x07) {
				CheckOtherTradeRound(BitConverter.ToUInt16(bytes));

				var slot = BitConverter.ToUInt16(bytes, 6);// 对方交易栏槽位，0-4是格子，5是金币
				var count = BitConverter.ToUInt32(bytes, 8);// 物品数量

				if (slot == 5) {
					tradeGil[1] = count;
				} else {
					var itemId = BitConverter.ToUInt16(bytes, 0x10);
					Task.Run(() => {
						var item = GetItemFromList(itemId);
						tradeItemList[1][slot] = new TradeItem() {
							Id = itemId,
							Icon = item?.Icon,
							Count = count,
							Quality = false,
							Name = item?.Name.RawString,
							StackSize = item?.StackSize ?? 0
						};
					});
				}
			}
		}
		/// <summary>
		/// 检查对方交易栏数据包序号是否更新，更新则需要清除现有数据重新记录
		/// </summary>
		/// <param name="index">序号，暂时认为是2字节</param>
		private void CheckOtherTradeRound(ushort index) {
			if (index == targetRound)
				return;
			targetRound = index;
			tradeItemList[1] = new TradeItem[5];
			tradeGil[1] = 0;
		}
		private Item? GetItemFromList(uint id) {
			return Dalamud.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(r => r.RowId == id);
		}
		/// <summary>
		/// 交易结算
		/// </summary>
		/// <param name="status">交易状态</param>
		private void Finish(bool status) {
			// TODO 结算交易
			uint[] gil = new uint[] { tradeGil[0], tradeGil[1] };
			TradeItem[][] list = new TradeItem[2][] {
				new TradeItem[tradeItemList[0].Count(i => i.Id != 0)],
				new TradeItem[tradeItemList[1].Count(i => i.Id != 0)]
			};
			for (int i = 0, index = 0; i < tradeItemList[0].Length; i++) {
				if (tradeItemList[0][i].Id != 0) {
					list[0][index++] = new TradeItem() {
						Id = tradeItemList[0][i].Id,
						Quality = tradeItemList[0][i].Quality,
						Count = tradeItemList[0][i].Count,
						StackSize = tradeItemList[0][i].StackSize,
						Name = tradeItemList[0][i].Name,
					};
				}
			}
			for (int i = 0, index = 0; i < tradeItemList[1].Length; i++) {
				if (tradeItemList[1][i].Id != 0) {
					list[1][index++] = new TradeItem() {
						Id = tradeItemList[1][i].Id,
						Quality = tradeItemList[1][i].Quality,
						Count = tradeItemList[1][i].Count,
						StackSize = tradeItemList[1][i].StackSize,
						Name = tradeItemList[1][i].Name,
					};
				}
			}

			if (lastTarget != target) {
				multiGil = new uint[2] { 0, 0 };
				multiItemList = new Dictionary<uint, RecordItem>[2] { new(), new() };
			}
			// 如果交易成功，将内容累积进数组
			if (status) {
				multiGil[0] += tradeGil[0];
				multiGil[1] += tradeGil[1];
				foreach (TradeItem item in list[0]) {
					RecordItem rec;
					if (multiItemList[0].ContainsKey(item.Id)) {
						rec = multiItemList[0][item.Id];
					} else {
						rec = new() {
							Id = item.Id,
							Name = item.Name ?? "<Failed>",
							NqCount = 0,
							HqCount = 0,
							StackSize = item.StackSize,
						};
					}
					if (item.Quality) {
						rec.HqCount += item.Count;
					} else {
						rec.NqCount += item.Count;
					}
					multiItemList[0][item.Id] = rec;
				}
				foreach (TradeItem item in list[1]) {
					RecordItem rec;
					if (multiItemList[1].ContainsKey(item.Id)) {
						rec = multiItemList[1][item.Id];
					} else {
						rec = new() {
							Id = item.Id,
							Name = item.Name ?? "<Failed>",
							NqCount = 0,
							HqCount = 0,
							StackSize = item.StackSize,
						};
					}
					if (item.Quality) {
						rec.HqCount += item.Count;
					} else {
						rec.NqCount += item.Count;
					}
					multiItemList[1][item.Id] = rec;
				}
			}

			if (lastTarget == target) {
				Dalamud.ChatGui.Print(BuildMultiTradeSeString(status, target, list, gil, multiItemList, multiGil).BuiltString);
			} else {
				Dalamud.ChatGui.Print(BuildTradeSeString(status, target, list, gil).BuiltString);
			}
			if (status)
				lastTarget = target;
		}
		/// <summary>
		/// 重置变量
		/// </summary>
		private unsafe void Reset() {
			tradeItemList = new TradeItem[2][] { new TradeItem[5], new TradeItem[5] };
			tradeGil = new uint[2];
			onceVisible = true;
			position = new int[2] { int.MinValue, int.MinValue };
			success = false;

			// 当自己发起交易时，似乎没有网络包能够获取到交易对象的ID
			byte* bytePtr = tradeBuddy.PluginUi.atkArrayDataHolder->StringArrays[9]->StringArray[11];
			var len = 0;
			while (len < 100 && bytePtr[len] != 0) {
				len++;
			}
			target = SeString.Parse(bytePtr, len).TextValue;
		}
		private unsafe void NetworkMessageDelegate(IntPtr dataPtr, ushort opcode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
			// 判断交易窗口是否存在
			if (direction == NetworkMessageDirection.ZoneDown && opcode == config.OpcodeOfTradeForm) {
				var code = Marshal.ReadInt32(dataPtr);
				if (trading && code == 0) {
					// 00 00 00 00 窗口关闭
					trading = false;
					Finish(success);
					PluginLog.Debug("交易结束");
				} else if (!trading && code == 0x1000) {
					// 00 10 00 00 交易窗口激活
					Reset();
					trading = true;
					PluginLog.Debug("交易开始");
				}
			}

			if (trading) {
				if (direction == NetworkMessageDirection.ZoneUp && opcode == config.OpcodeOfInventoryModifyHandler) {
					if (Marshal.ReadByte(dataPtr, 03) == 0x10 && Marshal.ReadByte(dataPtr, 05) == 03)
						UpdateMyTradeSlot(Marshal.PtrToStructure<InventoryModifyHandler>(dataPtr));
				} else if (direction == NetworkMessageDirection.ZoneDown && opcode == config.OpcodeOfItemInfo) {
					byte[] bytes = new byte[0x21];
					Marshal.Copy(dataPtr, bytes, 0, bytes.Length);
					UpdateOtherTradeItem(bytes);
				} else if (direction == NetworkMessageDirection.ZoneDown && opcode == config.OpcodeOfCurrencyCrystalInfo) {
					byte[] bytes = new byte[0x12];
					Marshal.Copy(dataPtr, bytes, 0, bytes.Length);
					UpdateOtherTradeMoney(bytes);
				} else if (direction == NetworkMessageDirection.ZoneDown && opcode == config.OpcodeOfUpdateInventorySlot) {
					success = true;
				}
			}
		}
		/// <summary>
		/// 输出单次交易的内容
		/// </summary>
		/// <param name="status">交易状态</param>
		/// <param name="target">交易目标</param>
		/// <param name="items">交易物品</param>
		/// <param name="gil">交易金币</param>
		/// <returns></returns>
		private static SeStringBuilder BuildTradeSeString(bool status, string target, TradeItem[][] items, uint[] gil) {
			if (items.Length == 0 || gil.Length == 0) {
				return new SeStringBuilder()
				.AddText($"[{TradeBuddy.PluginName}]")
				.AddUiForeground("获取交易内容失败", 17);
			}
			var builder = new SeStringBuilder()
				.AddText($"[{TradeBuddy.PluginName}]" + (char)SeIconChar.ArrowRight)
				.AddText(target);// TODO 点击名字能够查询到该角色的过往交易记录
			if (!status)
				builder.AddText("(取消)");
			if (gil[0] != 0 || items[0].Length != 0) {
				builder.AddText("\n");
				// Get
				builder.AddText("<<==  ");
				if (gil[0] != 0)
					builder.AddText($"{gil[0]:#,0}{(char)SeIconChar.Gil}");
				if (gil[0] != 0 && items[0].Length != 0)
					builder.AddText(", ");
				for (int i = 0; i < items[0].Length; i++) {
					if (i != 0)
						builder.AddText(", ");
					var name = items[0][i].Name + (items[0][i].Quality ? SeIconChar.HighQuality.ToIconString() : string.Empty) + "x" + items[0][i].Count;
					builder.AddItemLink(items[0][i].Id, items[0][i].Quality)
						.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500)
						.AddUiForeground(name ?? "<Failed>", 1)
						.Add(RawPayload.LinkTerminator);
				}
			}
			if (gil[1] != 0 || items[1].Length != 0) {
				builder.AddText("\n");
				// Give
				builder.AddText("==>>  ");
				if (gil[1] != 0)
					builder.AddText($"{gil[1]:#,0}{(char)SeIconChar.Gil}");
				if (gil[1] != 0 && items[1].Length != 0)
					builder.AddText(", ");
				for (int i = 0; i < items[1].Length; i++) {
					if (i != 0)
						builder.AddText(", ");
					var name = items[1][i].Name + (items[1][i].Quality ? SeIconChar.HighQuality.ToIconString() : string.Empty) + "x" + items[1][i].Count;
					builder.AddItemLink(items[1][i].Id, items[1][i].Quality)
						.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500)
						.AddUiForeground(name ?? "<Failed>", 1)
						.Add(RawPayload.LinkTerminator);
				}
			}
			return builder;
		}
		/// <summary>
		/// 输出多次交易内容
		/// </summary>
		/// <param name="status">交易状态</param>
		/// <param name="target">交易目标</param>
		/// <param name="items">交易物品</param>
		/// <param name="gil">交易金币</param>
		/// <param name="multiItems">累积交易物品</param>
		/// <param name="multiGil">累积交易金币</param>
		/// <returns></returns>
		private static SeStringBuilder BuildMultiTradeSeString(bool status, string target, TradeItem[][] items, uint[] gil, Dictionary<uint, RecordItem>[] multiItems, uint[] multiGil) {
			if (items.Length == 0 || gil.Length == 0 || multiItems.Length == 0 || multiGil.Length == 0) {
				return new SeStringBuilder()
				.AddText($"[{TradeBuddy.PluginName}]")
				.AddUiForeground("获取交易内容失败", 17);
			}
			var builder = BuildTradeSeString(status, target, items, gil);
			builder.AddText("\n连续交易:");

			// 如果金币和物品都没有，则略过该行为，不输出
			if (multiGil[0] != 0 || multiItems[0].Count != 0) {
				builder.AddText("\n");
				// Get
				builder.AddText("<<==  ");
				if (multiGil[0] != 0)
					builder.AddText($"{multiGil[0]:#,0}{(char)SeIconChar.Gil}");

				foreach (var itemId in multiItems[0].Keys) {
					var item = multiItems[0][itemId];
					if (multiGil[0] != 0)
						builder.AddText(", ");

					builder.AddItemLink(itemId, item.NqCount == 0)
						.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500)
						.AddUiForeground(item.Name, 1);

					PluginLog.Debug($"累积收到组{multiItems[0].Count}[id={itemId},name={item.Name}]:{item.NqCount},{item.HqCount},{item.StackSize}");
					var nqStr = item.NqCount >= item.StackSize ? (item.NqCount / item.StackSize + "组" + item.NqCount % item.StackSize) : item.NqCount.ToString("#,0");
					var hqStr = item.HqCount >= item.StackSize ? (item.HqCount / item.StackSize + "组" + item.HqCount % item.StackSize) : item.HqCount.ToString("#,0");

					if (item.HqCount == 0) {
						builder.AddUiForeground($"<{nqStr}>", 1);
					} else if (item.NqCount == 0) {
						builder.AddUiForeground($"<{SeIconChar.HighQuality.ToIconString()} {hqStr}>", 1);
					} else {
						builder.AddUiForeground($"<{nqStr}/{SeIconChar.HighQuality.ToIconString()} {hqStr}>", 1);
					}
					builder.Add(RawPayload.LinkTerminator);
				}
			}
			if (multiGil[1] != 0 || multiItems[1].Count != 0) {
				builder.AddText("\n");
				// Give
				builder.AddText("==>>  ");
				if (multiGil[1] != 0)
					builder.AddText($"{multiGil[1]:#,0}{(char)SeIconChar.Gil}");

				foreach (var itemId in multiItems[1].Keys) {
					var item = multiItems[1][itemId];
					if (multiGil[1] != 0)
						builder.AddText(", ");

					builder.AddItemLink(itemId, item.NqCount == 0)
						.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500)
						.AddUiForeground(item.Name, 1);

					PluginLog.Debug($"累积给出组{multiItems[1].Count}[id={itemId},name={item.Name}]:{item.NqCount},{item.HqCount},{item.StackSize}");
					var nqStr = item.NqCount >= item.StackSize ? (item.NqCount / item.StackSize + "组" + item.NqCount % item.StackSize) : item.NqCount.ToString("#,0");
					var hqStr = item.HqCount >= item.StackSize ? (item.HqCount / item.StackSize + "组" + item.HqCount % item.StackSize) : item.HqCount.ToString("#,0");

					if (item.HqCount == 0) {
						builder.AddUiForeground($"<{nqStr}>", 1);
					} else if (item.NqCount == 0) {
						builder.AddUiForeground($"<{SeIconChar.HighQuality.ToIconString()} {hqStr}>", 1);
					} else {
						builder.AddUiForeground($"<{nqStr}/{SeIconChar.HighQuality.ToIconString()} {hqStr}>", 1);
					}
					builder.Add(RawPayload.LinkTerminator);
				}
			}
			return builder;
		}
	}
}
