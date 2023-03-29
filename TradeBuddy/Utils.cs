using Dalamud.Interface;
using ImGuiNET;

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
	}
}
