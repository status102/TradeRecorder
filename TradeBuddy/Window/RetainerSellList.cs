﻿using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using TradeBuddy.Model;

namespace TradeBuddy.Window
{
	public class RetainerSellList : IWindow
	{
		public readonly static byte[] START_BYTE = new byte[] { 0x02, 0x48, 0x04, 0xF2, 0x02, 0x25, 0x03, 0x02, 0x49, 0x04, 0xF2, 0x02, 0x26, 0x03 };
		public readonly static byte[] END_BYTE = new byte[] { 0x02, 0x49, 0x02, 0x01, 0x03, 0x02, 0x48, 0x02, 0x01, 0x03 };

		private Dictionary<string, float> priceList = new();
		private readonly TradeBuddy TradeBuddy;
		private Configuration Config => TradeBuddy.Configuration;

		public RetainerSellList(TradeBuddy tradeBuddy) {
			TradeBuddy = tradeBuddy;
		}

		public unsafe void Draw() {
			var sellListFormAddess = TradeBuddy.GameGui.GetAddonByName("RetainerSellList", 1);
			if (sellListFormAddess == IntPtr.Zero) {
				priceList.Clear();
				return;
			}

			var sellListForm = (AtkUnitBase*)sellListFormAddess;
			if (sellListForm->UldManager.LoadedState == AtkLoadState.Loaded && sellListForm->UldManager.NodeListCount == 20) {
				var sellList = sellListForm->UldManager.NodeList[10];
				if (sellList->GetAsAtkComponentNode()->Component->UldManager.NodeListCount == 17) {
					for (int i = 1; i < 14; i++) {
						var sellListItem = sellList->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];

						if (sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeListCount == 12) {
							var iconNode = (AtkComponentIcon*)sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeList[11]->GetComponent();
							if (iconNode->IconId == -1)
								continue;
							bool isHQ = Convert.ToString(iconNode->IconId).StartsWith("10");

							var priceNode = sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeList[9]->GetAsAtkTextNode();
							var nameNode = sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode();

							byte* bytePtr = nameNode->NodeText.InlineBuffer;

							byte[] byteBuffer = new byte[100];
							Array.Fill<byte>(byteBuffer, 0);
							int byteLen = 0;
							for (int byteIndex = 0; byteIndex < 99 && bytePtr[byteIndex] != 0; byteIndex++) {
								byteLen = byteIndex + 1;
								byteBuffer[byteIndex] = bytePtr[byteIndex];
							}
							if (byteLen <= 24) {
								StringBuilder sb = new();
								foreach (byte item in byteBuffer)
									sb.Append(' ').Append(PluginUI.intToHex[item / 16]).Append(PluginUI.intToHex[item % 16]);
#if DEBUG
								PluginLog.Error(String.Format("雇员出售列表解析错误len：{0:}，str：{1:}", byteLen, sb.ToString()));
#endif
							} else {
								byte[] strBuffer = new byte[byteLen - 24];
								Array.Copy(byteBuffer, 14, strBuffer, 0, byteLen - 24);
								string name = "";
								//todo 增加异常处理
								name = Encoding.UTF8.GetString(strBuffer).Replace("", "HQ");

								if (priceList.ContainsKey(name)) {
									try {
										//获取到雇员出售列表里面的价格
										int price = Convert.ToInt32(priceNode->NodeText.ToString().Replace(",", "").TrimEnd('').Trim());

										if (price >= priceList[name] && Config.DrawRetainerSellListProper) {
											//雇员出售价格合适
											priceNode->TextColor.R = (byte)Config.SellList.ProperColorArray[0];
											priceNode->TextColor.G = (byte)Config.SellList.ProperColorArray[1];
											priceNode->TextColor.B = (byte)Config.SellList.ProperColorArray[2];
										} else if (price < priceList[name] && Config.DrawRetainerSellListAlert) {
											//雇员出售价格过低
											priceNode->TextColor.R = (byte)Config.SellList.AlertColorArray[0];
											priceNode->TextColor.G = (byte)Config.SellList.AlertColorArray[1];
											priceNode->TextColor.B = (byte)Config.SellList.AlertColorArray[2];
										} else {
											priceNode->TextColor.R = (byte)Configuration.RetainerSellList.Color_Default[0];
											priceNode->TextColor.G = (byte)Configuration.RetainerSellList.Color_Default[1];
											priceNode->TextColor.B = (byte)Configuration.RetainerSellList.Color_Default[2];
										}
									} catch (FormatException e) {
										PluginLog.Error("雇员出售列表道具价格解析错误" + priceNode->NodeText.ToString() + "\n" + e.ToString());
									}
								} else if (Config.PresetItemDictionary.ContainsKey(name)) {
									foreach (PresetItem presetItem in Config.PresetItemList) {
										if (presetItem.ItemName == name) {
											if (presetItem.EvaluatePrice() != 0)
												priceList.Add(presetItem.ItemName, presetItem.EvaluatePrice());
											break;
										}
									}
								} else {
									priceNode->TextColor.R = (byte)Configuration.RetainerSellList.Color_Default[0];
									priceNode->TextColor.G = (byte)Configuration.RetainerSellList.Color_Default[1];
									priceNode->TextColor.B = (byte)Configuration.RetainerSellList.Color_Default[2];
								}
							}
						}
					}
				}
			}
		}

		public void Dispose() { }
	}
}