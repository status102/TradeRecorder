using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using System;
using System.Numerics;
using TradeBuddy.Window;
using static Dalamud.Game.Gui.ChatGui;

namespace TradeBuddy
{
	public class PluginUI : IDisposable
	{
		private Configuration configuration;

		private bool visible = false;
		public bool Visible
		{
			get { return this.visible; }
			set { this.visible = value; }
		}

		private bool settingsVisible = false;
		public bool SettingsVisible
		{
			get { return this.settingsVisible; }
			set { this.settingsVisible = value; }
		}

		public bool historyVisible = false;//交易历史界面是否可见

		public bool tradeOnceVisible = true;//保存单次交易时，监控窗口是否显示

		public bool finalCheck = false;//在双方都确认的情况下进入最终交易确认

		public PluginUI(Configuration configuration)
		{
			this.configuration = configuration;
			
			DalamudDll.ChatGui.ChatMessage += Trade.messageDelegate;
		}

		public void Dispose()
		{
			this.configuration.Dispose();
			DalamudDll.ChatGui.ChatMessage -= Trade.messageDelegate;
		}

		public void Draw()
		{
			// This is our only draw handler attached to UIBuilder, so it needs to be
			// able to draw any windows we might have open.
			// Each method checks its own visibility/state to ensure it only draws when
			// it actually makes sense.
			// There are other ways to do this, but it is generally best to keep the number of
			// draw delegates as low as possible.
			
			//RetainerSellList.Draw();
			Plugin.Instance.Setting.DrawSetting(ref settingsVisible);
			Trade.DrawTrade(configuration.ShowTrade, ref tradeOnceVisible, ref finalCheck, ref historyVisible, ref settingsVisible);
			Plugin.Instance.History.DrawHistory(ref historyVisible);
		}


		public void DrawSettingsWindow()
		{
			if (!SettingsVisible)
			{
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
			if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
				ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
			{
				// can't ref a property, so use a local copy
				
			}
			ImGui.End();
		}

	}
}
