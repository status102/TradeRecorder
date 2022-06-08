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

		public bool tradeOnceVisible = true;//保存单次交易时，监控窗口是否显示

		public bool finalCheck = false;//在双方都确认的情况下进入最终交易确认

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
				tradeOnceVisible = true;//交易窗口关闭后重置单次关闭设置
				finalCheck = false;//最终确认时取消
				return;
			}
			if (!tradeOnceVisible) return;
			var trade = (AtkUnitBase*)tradeAddess;
			if (trade->UldManager.NodeListCount <= 0) return;
			if (trade->UldManager.LoadedState != 3) return;//等待交易窗口加载完毕

			int width = 480;
			ImGui.SetNextWindowSize(new Vector2(width, 600), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(trade->X - width, trade->Y), ImGuiCond.Appearing);
			if (ImGui.Begin("测试窗口", ref this.tradeOnceVisible))
			{
				/*
				var justhaveatry = this.configuration.ShowTrade;
				if (ImGui.Checkbox("Checkbox", ref justhaveatry))
				{
					this.configuration.ShowTrade = justhaveatry;
				}
				*/
				string[] header = new string[] { "物品", "数量", "预期金额" };
				var receiveTarget = trade->UldManager.NodeList[20]->GetAsAtkTextNode()->NodeText;
				var receiveMoney = trade->UldManager.NodeList[6]->GetAsAtkTextNode();
				var receiveChecked = trade->UldManager.NodeList[31]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode()->AtkResNode.ScaleY == 1;

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

				AtkResNode*[] giveArray = new AtkResNode*[]
				{
					trade->UldManager.NodeList[30]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[29]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[28]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[27]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2],
					trade->UldManager.NodeList[26]->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]
				};

				if (giveChecked)
					ImGui.Text("支付方：√");
				else
					ImGui.Text("支付方：");


				DrowTradeTable("支付", 3, new float[] { -1, 150, 300 }, header, giveArray, giveMoney);
				ImGui.Text("支付：" + giveMoney->NodeText.ToString() + "p");
				ImGui.Spacing();
				if (receiveChecked)
					ImGui.Text("接收方<" + receiveTarget + ">：√");
				else
					ImGui.Text("接收方<" + receiveTarget + ">：");

				DrowTradeTable("接收", 3, new float[] { -1, 150, 300 }, header, receiveArray, receiveMoney);

				ImGui.Text("接收：" + receiveMoney->NodeText.ToString() + "p");

				if(!finalCheck &&receiveChecked && giveChecked)//双方交易确认，进入最终确认状态
				{
					var selectYesno = Plugin.plugin.GameGui.GetAddonByName("SelectYesno", 1);
					if (selectYesno != IntPtr.Zero && ((AtkUnitBase*)selectYesno)->UldManager.LoadedState == 3)
					{
						var yesButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[11];
						var noButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[8];
						//var noButton = ((AtkUnitBase*)selectYesno)->UldManager.NodeList[8];
						yesButton->AddEvent(AtkEventType.MouseClick, ,)
						finalCheck = true;
					}
				}
			}
			ImGui.End();
		}

		//绘制交易物品表格
		private unsafe void DrowTradeTable(String title, int col, float[] width, String[] headerText, AtkResNode*[] atkResNodeList, AtkTextNode* textNode)
		{
			AtkResNode* resNode;
			long iconId;
			int count;
			string iconIdStr, itemName, countStr;

			// todo 加入物品图片
			//绘制5个道具栏
			ImGui.BeginTable(title, col, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);

			for (int i = 0; i < col; i++)
			{
				ImGui.TableNextColumn();
				ImGui.TableHeader(headerText[i]);

				// todo 最好能让拉伸的时候只拉伸第一列
				if (width.Length > i && width[i] >= 0)
					ImGui.TableSetupColumn(headerText[i], ImGuiTableColumnFlags.WidthMask, width[i]);
				else
					ImGui.TableSetupColumn(headerText[i]);
			}

			for (int i = 0; i < atkResNodeList.Length; i++)
			{
				resNode = atkResNodeList[i];
				iconId = ((AtkComponentIcon*)resNode->GetComponent())->IconId;

				//放入交易格子中再取消时，iconId会残留为之前的数值
				if (!resNode->IsVisible || iconId < 1)
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

				// todo 计算预期金额
				ImGui.TableNextColumn();
				ImGui.Text("---");
			}

			ImGui.EndTable();

		}
	}
}
