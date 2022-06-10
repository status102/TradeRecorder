using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
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

		private ImGuiScene.TextureWrap goatImage;

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

		private bool tradeVisible = false;
		public bool TradeVisible
		{
			get { return this.tradeVisible; }
			set { this.tradeVisible = value; }
		}

		public bool historyVisible = false;//交易历史界面是否可见

		public bool tradeOnceVisible = true;//保存单次交易时，监控窗口是否显示

		public bool finalCheck = false;//在双方都确认的情况下进入最终交易确认

		public PluginUI(Configuration configuration, ImGuiScene.TextureWrap goatImage)
		{
			this.configuration = configuration;
			this.goatImage = goatImage;
			
			DalamudDll.ChatGui.ChatMessage += TradeUI.messageDelegate;
		}

		public void Dispose()
		{
			this.goatImage.Dispose();
			DalamudDll.ChatGui.ChatMessage -= TradeUI.messageDelegate;
		}

		public void Draw()
		{
			// This is our only draw handler attached to UIBuilder, so it needs to be
			// able to draw any windows we might have open.
			// Each method checks its own visibility/state to ensure it only draws when
			// it actually makes sense.
			// There are other ways to do this, but it is generally best to keep the number of
			// draw delegates as low as possible.

			DrawMainWindow();
			DrawSettingsWindow();
			TradeUI.DrawTrade(ref tradeOnceVisible, ref finalCheck, ref historyVisible, ref settingsVisible);
			HistoryUI.DrawHistory(ref historyVisible);
		}

		public void DrawMainWindow()
		{
			if (!Visible) return;

			ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
			if (ImGui.Begin("My Amazing Window", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
			{
				ImGui.Text("案件数量：" + ImGui.GetIO().KeysDown[37] + "案件数量：" + ImGui.GetIO().KeysDown[38] + "案件数量：" + ImGui.GetIO().KeysDown[39] + "案件数量：" + ImGui.GetIO().KeysDown[40]);

				ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowLeft);
				ImGuiComponents.TextWithLabel("label", "value", "hint");
				ImGui.Text($"The random config bool is {this.configuration.SomePropertyToBeSavedAndWithADefault}");

				if (ImGui.Button("Show Settings"))
				{
					SettingsVisible = true;
				}

				ImGui.Spacing();

				ImGui.Text("Have a goat:");
				ImGui.Indent(55);
				ImGui.Image(this.goatImage.ImGuiHandle, new Vector2(this.goatImage.Width, this.goatImage.Height));
				ImGui.Unindent(55);
			}
			ImGui.End();
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
				var configValue = this.configuration.SomePropertyToBeSavedAndWithADefault;
				if (ImGui.Checkbox("Random Config Bool", ref configValue))
				{
					this.configuration.SomePropertyToBeSavedAndWithADefault = configValue;
					// can save immediately on change, if you don't want to provide a "Save and Close" button
					this.configuration.Save();
				}
			}
			ImGui.End();
		}

	}
}
