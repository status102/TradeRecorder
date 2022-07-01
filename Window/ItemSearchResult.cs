using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Text;

namespace TradeBuddy.Window
{
	public class ItemSearchResult
	{
		private bool addition = false;

		/// <summary>
		/// 板子出售列表显示预期价格
		/// </summary>
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
				if (len <= 24) PluginLog.Error("ItemSearchResult物品文本解析错误，len=" + len);
				else
				{
					byte[] strBuffer = new byte[len - 24];
					Array.Copy(byteBuffer, 14, strBuffer, 0, len - 24);
					string itemName = Encoding.UTF8.GetString(strBuffer);
					string priceNQStr = "", priceHQStr = "";

					if (Plugin.Instance.Configuration.PresetItemDictionary.ContainsKey(itemName))
						priceNQStr = Plugin.Instance.Configuration.PresetItemList[Plugin.Instance.Configuration.PresetItemDictionary[itemName]].GetPriceStr();

					if (Plugin.Instance.Configuration.PresetItemDictionary.ContainsKey(itemName + "HQ"))
						priceHQStr = Plugin.Instance.Configuration.PresetItemList[Plugin.Instance.Configuration.PresetItemDictionary[itemName + "HQ"]].GetPriceStr();

					if (!string.IsNullOrEmpty(priceNQStr)) priceNQStr = String.Format("  NQ：{0:}", priceNQStr);
					if (!string.IsNullOrEmpty(priceHQStr)) priceHQStr = String.Format("  HQ：{0:}", priceHQStr);
					if (!string.IsNullOrEmpty(priceHQStr) || !string.IsNullOrEmpty(priceNQStr))
					{
						itemName = String.Format("{0:}  预期价格-{1:}", itemName, (priceNQStr + priceHQStr)[2..]);
					}
					sellItemNameNode->SetText(itemName);
					sellItemNameNode->TextColor.R = 243;
					sellItemNameNode->TextColor.G = 243;
					sellItemNameNode->TextColor.B = 243;
				}
			}
		}
	}
}
