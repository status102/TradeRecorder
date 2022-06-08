using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;
using System.Numerics;

namespace TradeBuddy
{
	// It is good to have this be disposable in general, in case you ever need it
	// to do any cleanup
	public class PluginUI : IDisposable
	{
		private Configuration configuration;

		private ImGuiScene.TextureWrap goatImage;

		// this extra bool exists for ImGui, since you can't ref a property
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

		//保存单次交易时，监控窗口是否显示
		public bool tradeOnceVisible = true;

		// passing in the image here just for simplicity
		public PluginUI(Configuration configuration, ImGuiScene.TextureWrap goatImage)
		{
			this.configuration = configuration;
			this.goatImage = goatImage;
		}

		public void Dispose()
		{
			this.goatImage.Dispose();
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
			DrawTrade();
		}

		public void DrawMainWindow()
		{
			if (!Visible)
			{
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
			if (ImGui.Begin("My Amazing Window", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
			{
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

		private unsafe void DrawTrade()
		{
			var tradeAddess = Plugin.plugin.GameGui.GetAddonByName("Trade", 1);
			if (tradeAddess == IntPtr.Zero)
			{
				tradeOnceVisible = true;
				return;
			}
			if (!tradeOnceVisible) return;
			var trade = (AtkUnitBase*)tradeAddess;
			if (trade->UldManager.NodeListCount <= 0) return;
			if (trade->UldManager.LoadedState != 3) return;

			int width = 350;
			ImGui.SetNextWindowSize(new Vector2(width, 600), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(trade->X - width, trade->Y), ImGuiCond.Appearing);
			if (ImGui.Begin("测试窗口", ref this.tradeOnceVisible/*, ImGuiWindowFlags.NoCollapse*/))
			{

				var justhaveatry = this.configuration.ShowTrade;
				if (ImGui.Checkbox("Checkbox", ref justhaveatry))
				{
					this.configuration.ShowTrade = justhaveatry;
				}

				string[] header = new string[] { "物品", "数量", "预期金额" };
				//trade->CollisionNodeList[0]->CollisionType
				var receiveTarget = trade->UldManager.NodeList[20]->GetAsAtkTextNode()->NodeText;
				var receiveMoney = trade->UldManager.NodeList[6]->GetAsAtkTextNode();
				var receiveChecked = trade->UldManager.NodeList[31]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode()->AtkResNode.ScaleY == 1;
				/*
				var receiveItem1 = (AtkComponentIcon*)trade->UldManager.NodeList[19]->GetComponent();
				var receiveItem2 = (AtkComponentIcon*)trade->UldManager.NodeList[18]->GetComponent();
				var receiveItem3 = (AtkComponentIcon*)trade->UldManager.NodeList[17]->GetComponent();
				var receiveItem4 = (AtkComponentIcon*)trade->UldManager.NodeList[16]->GetComponent();
				var receiveItem5 = (AtkComponentIcon*)trade->UldManager.NodeList[15]->GetComponent();
				*/
				AtkResNode*[] receiveArray = new AtkResNode*[]
				{
					trade->UldManager.NodeList[19],
					trade->UldManager.NodeList[18],
					trade->UldManager.NodeList[17],
					trade->UldManager.NodeList[16],
					trade->UldManager.NodeList[15]
				};

				var giveMoney = trade->UldManager.NodeList[24]->GetAsAtkComponentNode()->Component->UldManager.NodeList[3]->GetAsAtkTextNode();

				//var giveItem1Back = trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]->GetAsAtkImageNode();
				//var giveItem1Pic = trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode();
				var giveChecked = trade->UldManager.NodeList[32]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode()->AtkResNode.ScaleY == 1;

				var giveItem1 = (AtkComponentIcon*)trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetComponent();
				var giveItem2 = (AtkComponentIcon*)trade->UldManager.NodeList[29]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetComponent();
				var giveItem3 = (AtkComponentIcon*)trade->UldManager.NodeList[28]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetComponent();
				var giveItem4 = (AtkComponentIcon*)trade->UldManager.NodeList[27]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetComponent();
				var giveItem5 = (AtkComponentIcon*)trade->UldManager.NodeList[26]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetComponent();

				AtkResNode*[] giveArray = new AtkResNode*[]
				{
					trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[29]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[28]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[27]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[26]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]
				};

				Item? item1;

				if (giveChecked)
					ImGui.Text("支付方：√");
				else
					ImGui.Text("支付方：");

				
				DrowNewTextRow("支付", 3, header, giveArray);
				ImGui.Text("支付：" + giveMoney->NodeText.ToString() + "p");

				if (receiveChecked)
					ImGui.Text("接收方<" + receiveTarget + ">：√");
				else
					ImGui.Text("接收方<" + receiveTarget + ">：");

				//DrowNewTextRow("接收", 3, header, receiveArray);

				ImGui.Text("接收：" + receiveMoney->NodeText.ToString() + "p");

			}
			ImGui.End();
		}

		//向正在从创建的表中追加
		private unsafe void DrowNewTextRow(String title, int col, String[] headerText, AtkResNode*[] atkResNodeList)
		{
			AtkResNode* resNode;
			long iconId;
			int count;
			string iconIdStr, itemName, countStr;

			ImGui.BeginTable(title, col, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg| ImGuiTableFlags.BordersInnerH);
			
			for (int i = 0; i < col; i++)
			{
				ImGui.TableNextColumn();
				ImGui.TableHeader(headerText[i]);
			}
			ImGui.SetColumnWidth(1, 15);
			ImGui.SetColumnWidth(2, 25);

			for (int i = 0; i < atkResNodeList.Length; i++)
			{
				resNode = atkResNodeList[i];
				iconId = ((AtkComponentIcon*)resNode->GetComponent())->IconId;
				if (iconId < 1)
				{
					ImGui.TableNextColumn();
					ImGui.Text("");
					ImGui.TableNextColumn();
					ImGui.Text("");
					ImGui.TableNextColumn();
					ImGui.Text("");
					continue;
				}
				iconIdStr = Convert.ToString(iconId);

				ImGui.TableNextColumn();
				//ImGui.Text();

				if (iconIdStr.StartsWith("10"))//HQ物品
					iconId = Convert.ToInt64(iconIdStr[2..]);


				var item1 = Plugin.plugin.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(r => r.Icon == iconId);
				//item1 = Plugin.plugin.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(r => r.Icon == 10001);
				if (item1 != null)
				{
					if (iconIdStr.StartsWith("10"))
						itemName = item1.Name + "HQ";
					else
						itemName = item1.Name;
					ImGui.Text(itemName);
				}
				else
					ImGui.Text("获取失败");

				countStr = resNode->GetAsAtkComponentNode()->Component->UldManager.NodeList[6]->GetAsAtkTextNode()->NodeText.ToString();
				count = Convert.ToInt32(countStr);
				countStr = Convert.ToString(count);

				ImGui.TableNextColumn();
				ImGui.Text(countStr);


				ImGui.TableNextColumn();
				ImGui.Text("---");

			}

			ImGui.EndTable();
		}
	}
}
