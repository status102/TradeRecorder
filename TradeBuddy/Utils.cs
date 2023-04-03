using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Dalamud.Game.Text;

namespace TradeBuddy
{
	public class Utils
	{
		public static bool DrawIconButton(FontAwesomeIcon icon, int? id = null, string? label = null) {
			if (id != null)
				ImGui.PushID((int)id);
			ImGui.PushFont(UiBuilder.IconFont);
			string buildStr;
			if (label != null)
				buildStr = icon.ToIconString() + ("##" + label ?? "");
			else
				buildStr = icon.ToIconString();
			bool _return = ImGui.Button(buildStr);

			ImGui.PopFont();
			if (id != null)
				ImGui.PopID();
			return _return;
		}
		/// <summary>
		/// 获取背包某一格子的物品
		/// </summary>
		/// <param name="page">第几页，0起</param>
		/// <param name="index">序号，0起</param>
		/// <returns></returns>
		public unsafe static InventoryItem* GetInventoryItem(uint page, int index) {
			return InventoryManager.Instance()->GetInventoryContainer((InventoryType)page)->GetInventorySlot(index);
		}

		private static SeStringBuilder GetBaseString(string tag, string message, DalamudLinkPayload? payload = null) {
			if (payload is null) {
				return new SeStringBuilder()
					.AddUiForeground($"[{TradeBuddy.PluginName}] ", 45)
					.AddUiForeground($"[{tag}] ", 62)
					.AddText(message);
			} else {
				return new SeStringBuilder()
					.AddUiForeground($"[{TradeBuddy.PluginName}] ", 45)
					.AddUiForeground($"[{tag}] ", 62)
					.Add(payload)
					.AddUiForeground(message, 35)
					.Add(RawPayload.LinkTerminator);
				//SeIconChar
			}
		}
	}
}
