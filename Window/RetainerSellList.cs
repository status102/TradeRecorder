using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace TradeBuddy.Window
{
	public class RetainerSellList
	{
		public readonly static byte[] startByte = new byte[] { 0x02, 0x48, 0x04, 0xF2, 0x02, 0x25, 0x03, 0x02, 0x49, 0x04, 0xF2, 0x02, 0x26, 0x03 };
		public readonly static byte[] endByte = new byte[] { 0x02, 0x49, 0x02, 0x01, 0x03, 0x02, 0x48, 0x02, 0x01, 0x03 };

		private Dictionary<string, float> priceList = new();
		public unsafe void Draw()
		{
			var sellListFormAddess = DalamudDll.GameGui.GetAddonByName("RetainerSellList", 1);
			if (sellListFormAddess == IntPtr.Zero)
			{
				priceList.Clear();
				return;
			}

			var sellListForm = (AtkUnitBase*)sellListFormAddess;
			if (sellListForm->UldManager.LoadedState == 3 && sellListForm->UldManager.NodeListCount == 20)
			{
				var sellList = sellListForm->UldManager.NodeList[10];
				if (sellList->GetAsAtkComponentNode()->Component->UldManager.NodeListCount == 17)
				{
					for (int i = 1; i < 14; i++)
					{
						var sellListItem = sellList->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];

						if (sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeListCount == 12)
						{
							var iconNode = (AtkComponentIcon*)sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeList[11]->GetComponent();
							if (iconNode->IconId == -1) continue;
							bool isHQ = Convert.ToString(iconNode->IconId).StartsWith("10");
							long iconId = iconNode->IconId;
							if (isHQ)
							{
								iconId = Convert.ToInt64(Convert.ToString(iconNode->IconId).Substring(2));
							}

							var priceNode = sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeList[9]->GetAsAtkTextNode();
							var nameNode = sellListItem->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode();

							byte* bytePtr = nameNode->NodeText.InlineBuffer;

							byte[] byteBuffer = new byte[100];
							Array.Fill<byte>(byteBuffer, 0);
							int byteLen = 0;
							for (int byteIndex = 0; byteIndex < 99 && bytePtr[byteIndex] != 0; byteIndex++)
							{
								byteLen = byteIndex + 1;
								byteBuffer[byteIndex] = bytePtr[byteIndex];
							}
							byte[] strBuffer = new byte[byteLen - 24];
							Array.Copy(byteBuffer, 14, strBuffer, 0, byteLen - 24);
							string name = "";
							//todo增加异常处理
							name = Encoding.UTF8.GetString(strBuffer).Replace("", "HQ");

							//if (isHQ) name = name + "HQ";
							if (priceList.ContainsKey(name))
							{
								try
								{
									//获取到雇员出售列表里面的价格
									int price = Convert.ToInt32(priceNode->NodeText.ToString().Replace(",", "").TrimEnd('').Trim());
									if (price >= priceList[name])
									{
										priceNode->TextColor.R = 10;
										priceNode->TextColor.G = 187;
										priceNode->TextColor.B = 10;
									}
									else
									{
										priceNode->TextColor.R = 255;
										priceNode->TextColor.G = 227;
										priceNode->TextColor.B = 158;
									}
								}
								catch (FormatException)
								{
								}
							}
							else if (Plugin.Instance.Configuration.presetItemDictionary.ContainsKey(name))
							{
								foreach (Configuration.PresetItem presetItem in Plugin.Instance.Configuration.presetItemList)
								{
									if (presetItem.name == name)
									{
										priceList.Add(presetItem.name, presetItem.price);
										break;
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
