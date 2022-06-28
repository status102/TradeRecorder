using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Text;

namespace TradeBuddy.Window
{
	public class ItemSearchResult
	{
		private bool addition = false;
		public unsafe void Draw()
		{
			var itemSearchResultPtr = DalamudDll.GameGui.GetAddonByName("ItemSearchResult", 1);
			if (itemSearchResultPtr == IntPtr.Zero)
			{
				addition = false;
				return;
			}
			if (addition) return;
			var sellListForm = (AtkUnitBase*)itemSearchResultPtr;
			if (sellListForm->UldManager.LoadedState == 3 && sellListForm->UldManager.NodeListCount == 29)
			{
				addition = true;

				var sellItemNameNode = sellListForm->UldManager.NodeList[22]->GetAsAtkTextNode();
				var sellItemNameNodeTextPtr = sellItemNameNode->NodeText.InlineBuffer;
				byte[] byteBuffer = new byte[100];
				Array.Fill<byte>(byteBuffer, 0);
				int len = 0;
				for (int i = 0; i < 100 && sellItemNameNodeTextPtr[i] != 0; i++)
				{
					len = i + 1;
					byteBuffer[i] = sellItemNameNodeTextPtr[i];
				}
				if (len <= 24) PluginLog.Error("ItemSearchResult物品文本解析错误，len=" + len, Array.Empty<object>());
				else
				{
					byte[] strBuffer = new byte[len - 24];
					Array.Copy(byteBuffer, 14, strBuffer, 0, len - 24);
					string itemName = Encoding.UTF8.GetString(strBuffer);
					string priceNQStr = "", priceHQStr = "";
					
					if (Plugin.Instance.Configuration.presetItemDictionary.ContainsKey(itemName))
					{
						priceNQStr = String.Format("{0:0,0.0}", Plugin.Instance.Configuration.presetItemList[Plugin.Instance.Configuration.presetItemDictionary[itemName]].price).TrimStart('0');
						if (priceNQStr.EndsWith(".0")) priceNQStr = priceNQStr[0..^2];
					}
					else if (Plugin.Instance.Configuration.presetItemDictionary.ContainsKey(itemName + "HQ"))
					{
						priceHQStr = String.Format("{0:0,0.0}", Plugin.Instance.Configuration.presetItemList[Plugin.Instance.Configuration.presetItemDictionary[itemName + "HQ"]].price).TrimStart('0');
						if (priceHQStr.EndsWith(".0")) priceHQStr = priceHQStr[0..^2];
					}
					if (!string.IsNullOrEmpty(priceNQStr)) itemName = String.Format("{0:}  预期价格-NQ：{1:}", itemName, priceNQStr);
					if (!string.IsNullOrEmpty(priceHQStr)) itemName = String.Format("{0:}  预期价格-HQ：{1:}", itemName, priceHQStr);
					sellItemNameNode->SetText(itemName);
					sellItemNameNode->TextColor.R = 243;
					sellItemNameNode->TextColor.G = 243;
					sellItemNameNode->TextColor.B = 243;
				}
			}
		}
	}
}
