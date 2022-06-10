using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TradeBuddy.Window
{
	public class HistoryUI
	{
		public static void DrawHistory(ref bool historyVisible)
		{
			ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.FirstUseEver);
			if(ImGui.Begin("交易历史", ref historyVisible))
			{

			}
		}
	}
}
