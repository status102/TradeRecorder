using Dalamud.Game.ClientState.Objects.SubKinds;
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
using TradeBuddy.Model;
using TradeBuddy.Universalis;

namespace TradeBuddy.Window
{
	public class Trade2
	{
		private TradeBuddy tradeBuddy { get; init; }
		/// <summary>
		/// 窗口大小
		/// </summary>
		private const int WIDTH = 540, HEIGHT = 560;
		private int[] position = new int[2];
		private bool onceVisible = true;
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
		
		/// <summary>
		/// 交易金币记录，0自己，1对面
		/// </summary>
		private uint[] tradeGil = new uint[2] { 0, 0 };
		/// <summary>
		/// 连续交易物品记录，Key itemId，Value (name, nq, hq, stackSize)
		/// </summary>
		private Dictionary<uint, RecordItem>[] multiItemList = new Dictionary<uint, RecordItem>[2] { new(), new() };
		private class RecordItem
		{
			public uint Id { get; private init; }
			public string Name { get; private init; }
			public uint NqCount { get; set; } = 0;
			public uint HqCount { get; set; } = 0;
			public uint StackSize { get; private init; }
			public RecordItem(uint id, string? name, uint stackSize) {
				Id = id;
				Name = name ?? "(Unknown)";
				StackSize = stackSize;
			}
		}
		private uint[] multiGil = new uint[2] { 0, 0 };
		private uint worldId = 0;
		private (uint, string, string) target = (0, "", "");
		private (uint, string, string) lastTarget = (0, "", "");
		/// <summary>
		/// 对方交易栏的发包序号，序号步进后需要清空道具栏
		/// </summary>
		private ushort targetRound = 0;

		#region Init
		private DalamudLinkPayload Payload { get; init; }
		private Configuration Config => tradeBuddy.Configuration;
		public Trade2(TradeBuddy tradeBuddy) {
			this.tradeBuddy = tradeBuddy;
			tradeBuddy.GameNetwork.NetworkMessage += NetworkMessageDelegate;
			Payload = tradeBuddy.PluginInterface.AddChatLinkHandler(0, OnTradeTargetClick);
		}
		public void Dispose() {
			tradeBuddy.GameNetwork.NetworkMessage -= NetworkMessageDelegate;
			tradeBuddy.PluginInterface.RemoveChatLinkHandler(0);
		}
		#endregion
		public unsafe void Draw() {
			if (!Config.ShowTrade || !trading || !onceVisible) { return; }
			if (position[0] == int.MinValue) {
				var address = tradeBuddy.GameGui.GetAddonByName("Trade", 1);
				if (address != IntPtr.Zero && ((AtkUnitBase*)address)->UldManager.LoadedState == AtkLoadState.Loaded) {
					position[0] = ((AtkUnitBase*)address)->X - WIDTH - 5;
					position[1] = ((AtkUnitBase*)address)->Y + 2;
				}
			}
			ImGui.SetNextWindowSize(new Vector2(WIDTH, HEIGHT), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(position[0], position[1]), ImGuiCond.Always);
			if (ImGui.Begin("玩家交易", ref onceVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)) {
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

				// 显示当前交易对象的记录
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.History)) { tradeBuddy.PluginUi.History.ShowHistory(target); }
				if (ImGui.IsItemHovered()) { ImGui.SetTooltip("显示当前交易对象的交易记录"); }

				// 显示设置窗口
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog)) { tradeBuddy.PluginUi.Setting.Show(); }

				DrawTradeTable(tradeItemList[0], tradeGil[0]);
				ImGui.Spacing();

				ImGui.TextUnformatted(target.Item2 + SeIconChar.CrossWorld.ToIconString() + target.Item3 + " -->");
				DrawTradeTable(tradeItemList[1], tradeGil[1]);
				ImGui.End();
			}
		}
		/// <summary>
		/// 绘制交易道具表
		/// </summary>
		/// <param name="items"></param>
		/// <param name="gil"></param>
		private void DrawTradeTable(TradeItem[] items, uint gil) {
			if (ImGui.BeginTable("交易栏", COL_NAME.Length, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV)) {

				for (int i = 0; i < COL_NAME.Length; i++) {
					if (COL_WIDTH.Length > i) {
						if (COL_WIDTH[i] >= 0)
							ImGui.TableSetupColumn(COL_NAME[i], ImGuiTableColumnFlags.WidthFixed, COL_WIDTH[i]);
						else
							ImGui.TableSetupColumn(COL_NAME[i], ImGuiTableColumnFlags.WidthStretch);
					}
				}
				ImGui.TableHeadersRow();
				for (int i = 0; i < items.Length; i++) {
					ImGui.TableNextRow(ImGuiTableRowFlags.None, ROW_HEIGHT);
					ImGui.TableNextColumn();

					if (items[i].Id == 0) {
						continue;
					}
					var icon = PluginUI.GetIcon(items[i].IconId ?? 0, items[i].Quality);
					if (icon != null) { ImGui.Image(icon.ImGuiHandle, IMAGE_SIZE); }

					ImGui.TableNextColumn();
					ImGui.TextUnformatted(items[i].Name);

					// TODO 右键
					if (ImGui.IsItemHovered()) {
						try {
							var itemPresetStr = Config.PresetItemList[Config.PresetItemDictionary[items[i].Name ?? ""]].GetPriceStr();
							//if (!string.IsNullOrEmpty(itemPresetStr))
							//ImGui.SetTooltip($"{itemList[i].priceName} 预设：{itemPresetStr}");
						} catch (KeyNotFoundException) { }
					}

					ImGui.TableNextColumn();
					ImGui.TextUnformatted(Convert.ToString(items[i].Count));

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
					// 显示大区最低价格
					ImGui.TableNextColumn();

					if (!items[i].Quality) {
						// NQ能够接受HQ价格
						items[i].MinPrice = Math.Min(items[i].ItemPrice.GetMinPrice(worldId).Item1, items[i].ItemPrice.GetMinPrice(worldId).Item2);
					} else {
						items[i].MinPrice = items[i].ItemPrice.GetMinPrice(worldId).Item2;
					}
					if (items[i].MinPrice > 0) {
						ImGui.TextUnformatted(items[i].MinPrice.ToString("#,0"));
						if (ImGui.IsItemHovered()) {
							ImGui.SetTooltip($"World: {items[i].ItemPrice.GetMinPrice(worldId).Item3}\nTime: " + DateTimeOffset.FromUnixTimeMilliseconds(items[i].ItemPrice?.GetMinPrice(worldId).Item4 ?? 0).LocalDateTime.ToString(Price.format));
						}
					} else {
						ImGui.TextUnformatted("---");
						if (ImGui.IsItemHovered() && items[i].ItemPrice.GetMinPrice(worldId).Item3.Length > 0) {
							ImGui.SetTooltip(items[i].ItemPrice.GetMinPrice(worldId).Item3);
						}
					}
				}

				ImGui.TableNextRow(ImGuiTableRowFlags.None, ROW_HEIGHT);
				ImGui.TableNextColumn();

				if (GIL_IMAGE != null) { ImGui.Image(GIL_IMAGE.ImGuiHandle, IMAGE_SIZE); }

				ImGui.TableNextColumn();
				ImGui.TextUnformatted($"{gil:#,0}");

				float sum = 0;
				uint min = gil;
				foreach (var item in items) { 
					if (item.MinPrice > 0) { min += (uint)item.MinPrice * item.Count; } 
				}

				ImGui.TableNextColumn();
				ImGui.TableNextColumn();
				//foreach (var item in itemArray)
				//sum += item.price;

				sum += gil;
				ImGui.TextUnformatted($"{sum:#,0}");
				if (ImGui.IsItemHovered()) { ImGui.SetTooltip($"包含金币在内的全部金额，单击复制：{sum:#,0}"); }
				if (ImGui.IsItemClicked()) { ImGui.SetClipboardText($"{sum:#,0}"); }

				ImGui.TableNextColumn();

				ImGui.TextUnformatted($"{min:#,0}");
				if (ImGui.IsItemHovered()) { ImGui.SetTooltip($"以最低价计算的金额，单击复制：{min:#,0}"); }
				if (ImGui.IsItemClicked()) { ImGui.SetClipboardText($"{min:#,0}"); }

				ImGui.EndTable();
			}
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
							tradeItemList[0][modify.Slot2] = new TradeItem(item->ItemID, 1, Convert.ToBoolean((uint)item->Flags & (uint)FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HQ));
						}
					} else if (modify.Page == 0x07D5 && modify.Page2 == 0x07D5) {
						// 交易栏内互相移动
						TradeItem a = tradeItemList[0][modify.Slot];
						tradeItemList[0][modify.Slot] = tradeItemList[0][modify.Slot2];
						tradeItemList[0][modify.Slot2] = a;
					} else if (modify.Page == 0x07D5) {
						// 删除某一栏道具
						tradeItemList[0][modify.Slot] = new TradeItem();
					} else if (modify.Page == 0x07D1) {
						// 水晶
						tradeItemList[0][modify.Slot2] = new TradeItem((uint)(modify.Slot + 2), modify.Count);
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
			// TODO 数据包拆离
			if (bytes[3] == 0x10 && bytes[4] == 0xD9 && bytes[5] == 0x07 && bytes[8] == 0xD9 && bytes[9] == 0x07) {
				CheckOtherTradeRound(BitConverter.ToUInt16(bytes));

				var slot = BitConverter.ToUInt16(bytes, 0x0A);
				var count = BitConverter.ToUInt32(bytes, 0x0C);
				var itemId = BitConverter.ToUInt16(bytes, 0x10);
				var isHq = bytes[0x20];

				tradeItemList[1][slot] = new TradeItem(itemId, count, Convert.ToBoolean(isHq));
			}
		}
		/// <summary>
		/// 处理对方交易栏的水晶、金币变化
		/// </summary>
		/// <param name="bytes"></param>
		private void UpdateOtherTradeMoney(byte[] bytes) {
			// TODO 数据包拆离
			if (bytes[3] == 0x10 && bytes[4] == 0xD9 && bytes[5] == 0x07) {
				CheckOtherTradeRound(BitConverter.ToUInt16(bytes));

				var slot = BitConverter.ToUInt16(bytes, 6);// 对方交易栏槽位，0-4是格子，5是金币
				var count = BitConverter.ToUInt32(bytes, 8);// 物品数量

				if (slot == 5) {
					tradeGil[1] = count;
				} else {
					var itemId = BitConverter.ToUInt16(bytes, 0x10);
					tradeItemList[1][slot] = new TradeItem(itemId, count);
				}
			}
		}
		/// <summary>
		/// 检查对方交易栏数据包序号是否更新，更新则需要清除现有数据重新记录
		/// </summary>
		/// <param name="index">序号，暂时认为是2字节</param>
		private void CheckOtherTradeRound(ushort index) {
			if (index == targetRound) { return; }
			targetRound = index;
			tradeItemList[1] = new TradeItem[5] { new(), new(), new(), new(), new() };
			tradeGil[1] = 0;
		}
		/// <summary>
		/// 交易结算
		/// </summary>
		/// <param name="status">交易状态</param>
		private void Finish(bool status) {
			uint[] gil = new uint[] { tradeGil[0], tradeGil[1] };
			TradeItem[][] list = new TradeItem[2][] {
				tradeItemList[0].Where(i => i.Id != 0).ToArray(),
				tradeItemList[1].Where(i => i.Id != 0).ToArray()
			};

			tradeBuddy.PluginUi.History.AddHistory(status, $"{target.Item2}@{target.Item3}", gil, list);

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
						rec = new(item.Id, item.Name, item.StackSize);
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
						rec = new(item.Id, item.Name, item.StackSize);
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
				DalamudInterface.ChatGui.Print(BuildMultiTradeSeString(Payload, status, target, list, gil, multiItemList, multiGil).BuiltString);
			} else {
				DalamudInterface.ChatGui.Print(BuildTradeSeString(Payload, status, target, list, gil).BuiltString);
			}
			if (status) { lastTarget = target; }
		}
		/// <summary>
		/// 重置变量
		/// </summary>
		private unsafe void Reset() {
			tradeItemList = new TradeItem[2][] { new TradeItem[5] { new(), new(), new(), new(), new() }, new TradeItem[5] { new(), new(), new(), new(), new() } };
			tradeGil = new uint[2];
			onceVisible = true;
			position = new int[2] { int.MinValue, int.MinValue };
			success = false;
			worldId = DalamudInterface.ClientState.LocalPlayer?.HomeWorld.Id ?? 0;
		}
		/// <summary>
		/// 交易目标id
		/// </summary>
		/// <param name="bytes"></param>
		private void ReceiveTargetID(byte[] bytes) {
			// TODO 数据包拆离
			if (bytes[4] == 0x10 && bytes[5] == 0x03) {
				var id = BitConverter.ToUInt32(bytes, 40);
				var player = DalamudInterface.ObjectTable.FirstOrDefault(i => i.ObjectId == id) as PlayerCharacter;
				if (player != null) {
					PluginLog.Debug($"获得到ID：[{id:X}-{player.Name}]");
					if (player.ObjectId != DalamudInterface.ClientState.LocalPlayer?.ObjectId) {
						var world = Utils.GetSever(player.HomeWorld.Id);
						target = (player.HomeWorld.Id, player.Name.TextValue, world?.Name ?? "<Unknown>");
					}
				}
			}
		}
		private unsafe void NetworkMessageDelegate(IntPtr dataPtr, ushort opcode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
			// 判断交易窗口是否存在
			if (direction == NetworkMessageDirection.ZoneDown && opcode == Config.OpcodeOfTradeForm) {
				var code = Marshal.ReadInt32(dataPtr);
				// TODO 调整交易判定
				// len:11
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
				if (direction == NetworkMessageDirection.ZoneUp && opcode == Config.OpcodeOfInventoryModifyHandler) {
					if (Marshal.ReadByte(dataPtr, 03) == 0x10 && Marshal.ReadByte(dataPtr, 05) == 03)
						UpdateMyTradeSlot(Marshal.PtrToStructure<InventoryModifyHandler>(dataPtr));
				} else if (direction == NetworkMessageDirection.ZoneDown && opcode == Config.OpcodeOfItemInfo) {
					byte[] bytes = new byte[0x21];
					Marshal.Copy(dataPtr, bytes, 0, bytes.Length);
					UpdateOtherTradeItem(bytes);
				} else if (direction == NetworkMessageDirection.ZoneDown && opcode == Config.OpcodeOfCurrencyCrystalInfo) {
					byte[] bytes = new byte[0x12];
					Marshal.Copy(dataPtr, bytes, 0, bytes.Length);
					UpdateOtherTradeMoney(bytes);
				} else if (direction == NetworkMessageDirection.ZoneDown && opcode == Config.OpcodeOfUpdateInventorySlot) {
					// 背包变动，交易成功且有物品进出
					success = true;
				}
			} else {
				if (direction == NetworkMessageDirection.ZoneDown && opcode == Config.OpcodeOfTradeTargetInfo) {
					byte[] bytes = new byte[45];
					Marshal.Copy(dataPtr, bytes, 0, bytes.Length);
					ReceiveTargetID(bytes);
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
		private static SeStringBuilder BuildTradeSeString(DalamudLinkPayload payload, bool status, (uint, string, string) target, TradeItem[][] items, uint[] gil) {
			if (items.Length == 0 || gil.Length == 0) {
				return new SeStringBuilder().AddText($"[{TradeBuddy.PluginName}]").AddUiForeground("获取交易内容失败", 17);
			}
			var builder = new SeStringBuilder()
				.AddText($"[{TradeBuddy.PluginName}]" + SeIconChar.ArrowRight.ToIconString())
				.Add(payload)
				.AddUiForeground(1).Add(new PlayerPayload(target.Item2, target.Item1)).AddUiForegroundOff();
			if (target.Item1 != DalamudInterface.ClientState.LocalPlayer?.HomeWorld.Id) { builder.Add(new IconPayload(BitmapFontIcon.CrossWorld)).AddText(target.Item3); }
			builder.Add(RawPayload.LinkTerminator);
			if (!status) { builder.AddUiForeground(" (取消)", 62); }
			// 获得
			if (gil[0] != 0 || items[0].Length != 0) {
				builder.Add(new NewLinePayload());
				builder.AddText("<<==  ");
				if (gil[0] != 0) { builder.AddText($"{gil[0]:#,0}{(char)SeIconChar.Gil}"); }
				for (int i = 0; i < items[0].Length; i++) {
					if (i != 0 || gil[0] != 0) { builder.AddText(", "); }
					var name = items[0][i].Name + (items[0][i].Quality ? SeIconChar.HighQuality.ToIconString() : string.Empty) + "x" + items[0][i].Count;
					builder.AddItemLink(items[0][i].Id, items[0][i].Quality)
						.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500)
						.AddUiForeground(name ?? "<Unknown>", 1)
						.Add(RawPayload.LinkTerminator);
				}
			}

			// 支付
			if (gil[1] != 0 || items[1].Length != 0) {
				builder.Add(new NewLinePayload());
				builder.AddText("==>>  ");
				if (gil[1] != 0) { builder.AddText($"{gil[1]:#,0}{(char)SeIconChar.Gil}"); }
				for (int i = 0; i < items[1].Length; i++) {
					if (i != 0 || gil[1] != 0) { builder.AddText(", "); }
					var name = items[1][i].Name + (items[1][i].Quality ? SeIconChar.HighQuality.ToIconString() : string.Empty) + "x" + items[1][i].Count;
					builder.AddItemLink(items[1][i].Id, items[1][i].Quality)
						.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500)
						.AddUiForeground(name ?? "<Unknown>", 1)
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
		private static SeStringBuilder BuildMultiTradeSeString(DalamudLinkPayload payload, bool status, (uint, string, string) target, TradeItem[][] items, uint[] gil, Dictionary<uint, RecordItem>[] multiItems, uint[] multiGil) {
			if (items.Length == 0 || gil.Length != 2 || multiItems.Length == 0 || multiGil.Length != 2) {
				return new SeStringBuilder().AddText($"[{TradeBuddy.PluginName}]").AddUiForeground("获取交易内容失败", 17);
			}
			var builder = BuildTradeSeString(payload, status, target, items, gil);
			builder.Add(new NewLinePayload()).AddText("连续交易:");

			// 如果金币和物品都没有，则略过该行为，不输出
			// 获得
			if (multiGil[0] != 0 || multiItems[0].Count != 0) {
				builder.Add(new NewLinePayload()).AddText("<<==  ");
				if (multiGil[0] != 0) { builder.AddText($"{multiGil[0]:#,0}{(char)SeIconChar.Gil}"); }

				foreach (var itemId in multiItems[0].Keys) {
					var item = multiItems[0][itemId];

					builder.AddItemLink(itemId, item.NqCount == 0);
					builder.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500);
					builder.AddUiForeground(item.Name, 1);

					var nqStr = item.StackSize > 1 && item.NqCount >= item.StackSize ? (item.NqCount / item.StackSize + "组" + item.NqCount % item.StackSize) : item.NqCount.ToString("#,0");
					var hqStr = item.StackSize > 1 && item.HqCount >= item.StackSize ? (item.HqCount / item.StackSize + "组" + item.HqCount % item.StackSize) : item.HqCount.ToString("#,0");

					if (item.HqCount == 0) {
						builder.AddUiForeground($"<{nqStr}>", 1);
					} else if (item.NqCount == 0) {
						builder.AddUiForeground($"<{SeIconChar.HighQuality.ToIconString()}{hqStr}>", 1);
					} else {
						builder.AddUiForeground($"<{nqStr}/{SeIconChar.HighQuality.ToIconString()}{hqStr}>", 1);
					}
					builder.Add(RawPayload.LinkTerminator);
				}
			}
			// 支付
			if (multiGil[1] != 0 || multiItems[1].Count != 0) {
				builder.Add(new NewLinePayload()).AddText("==>>  ");
				if (multiGil[1] != 0) { builder.AddText($"{multiGil[1]:#,0}{(char)SeIconChar.Gil}"); }

				foreach (var itemId in multiItems[1].Keys) {
					var item = multiItems[1][itemId];

					builder.AddItemLink(itemId, item.NqCount == 0);
					builder.AddUiForeground(SeIconChar.LinkMarker.ToIconString(), 500);
					builder.AddUiForeground(item.Name, 1);

					PluginLog.Debug($"累积给出组{multiItems[1].Count}[id={itemId},name={item.Name}]:{item.NqCount},{item.HqCount},{item.StackSize}");
					var nqStr = item.StackSize > 1 && item.NqCount >= item.StackSize ? (item.NqCount / item.StackSize + "组" + item.NqCount % item.StackSize) : item.NqCount.ToString("#,0");
					var hqStr = item.StackSize > 1 && item.HqCount >= item.StackSize ? (item.HqCount / item.StackSize + "组" + item.HqCount % item.StackSize) : item.HqCount.ToString("#,0");

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

		/// <summary>
		/// 点击输出内容中的交易对象
		/// </summary>
		/// <param name="commandId"></param>
		/// <param name="str"></param>
		public void OnTradeTargetClick(uint commandId, SeString str) {
			PlayerPayload? payload = (PlayerPayload?)str.Payloads.Find(i => i.Type == PayloadType.Player);
			if (payload != null) {
				tradeBuddy.PluginUi.History.ShowHistory((payload.World.RowId, payload.PlayerName, payload.World.Name.RawString));
			} else {
				Chat.PrintError("未找到交易对象");
				PluginLog.Verbose($"未找到交易对象，data=[{str.ToJson()}]");
			}
		}
	}
}
