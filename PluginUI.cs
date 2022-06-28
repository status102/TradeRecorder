using Dalamud.Game.Network;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TradeBuddy.Window;

namespace TradeBuddy
{
	public unsafe class PluginUI : IDisposable
	{
		public readonly static char[] intToHex = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		private Configuration configuration;
		public HistoryWindow History { get; init; }
		public Trade Trade { get; init; }
		public Setting Setting { get; init; }

		public RetainerSellList RetainerSellList { get; init; }
		public ItemSearchResult ItemSearchResult { get; init; }
		
		public AtkArrayDataHolder* atkArrayDataHolder { get; init; } = null;

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

		public StreamWriter? writer;

		public unsafe PluginUI(Configuration configuration)
		{
			/*
			try
			{
				FileStream stream = File.Open(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, String.Format("网络包{0:}.log", DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss"))), FileMode.OpenOrCreate);
				if (stream != null && stream.CanWrite)
					writer = new StreamWriter(stream);
			}
			catch (IOException e)
			{
				writer = null;
				DalamudDll.ChatGui.PrintError("网络包初始化：" + e.ToString());
			}*/
			this.configuration = configuration;

			Trade = new Trade();
			History = new HistoryWindow();
			Setting = new Setting();
			RetainerSellList = new RetainerSellList();
			ItemSearchResult = new ItemSearchResult();

			//if (writer == null) DalamudDll.ChatGui.Print("network初始化错误");

			var atkArrayDataHolder = &Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
			if (atkArrayDataHolder != null && atkArrayDataHolder->StringArrayCount > 0) this.atkArrayDataHolder = atkArrayDataHolder;

			DalamudDll.ChatGui.ChatMessage += Trade.MessageDelegate;
			//DalamudDll.GameNetwork.NetworkMessage += networkMessageDelegate;
		}

		public void Dispose()
		{
			this.configuration.Dispose();
			DalamudDll.ChatGui.ChatMessage -= Trade.MessageDelegate;
			//DalamudDll.GameNetwork.NetworkMessage -= networkMessageDelegate;

			if (writer != null)
			{
				writer.Flush();
				writer.Close();
			}
		}

		public void Draw()
		{
			// This is our only draw handler attached to UIBuilder, so it needs to be
			// able to draw any windows we might have open.
			// Each method checks its own visibility/state to ensure it only draws when
			// it actually makes sense.
			// There are other ways to do this, but it is generally best to keep the number of
			// draw delegates as low as possible.

			Trade.DrawTrade(configuration.ShowTrade, ref tradeOnceVisible, ref finalCheck, ref historyVisible, ref settingsVisible);
			History.DrawHistory(ref historyVisible);
			Setting.DrawSetting(ref settingsVisible);
			RetainerSellList.Draw();
			ItemSearchResult.Draw();
		}

		public unsafe void networkMessageDelegate(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
		{
			if (writer != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				byte databyte;
				stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff "));
				int index = Marshal.ReadByte(dataPtr, 36);
				int codeint56 = Marshal.ReadInt32(dataPtr, 56);
				ushort codeShort56 = (ushort)Marshal.ReadInt16(dataPtr, 56);
				ushort codeShort64 = (ushort)Marshal.ReadInt16(dataPtr, 64);
				ushort codeShort72 = (ushort)Marshal.ReadInt16(dataPtr, 72);

				stringBuilder.Append(String.Format("OpCode：{0:}，[{1:}->{2:}]", opCode, sourceActorId, targetActorId));
				stringBuilder.Append(string.Format("(index：{0:}, id(int56)：{1:}, id(short56)：{2:}, id(short64):{3:}, id(short64)：{4:})", index, codeint56, codeShort56, codeShort64, codeShort72));


				stringBuilder.Append("： ");
				for (int i = 0; i < 200; i++)
				{
					databyte = Marshal.ReadByte(dataPtr, i);
					stringBuilder.Append(' ').Append(intToHex[databyte / 16]).Append(intToHex[databyte % 16]);
				}
				writer.WriteLine(stringBuilder.ToString());
				//writer.WriteLine(Encoding.UTF8.GetString(databyte));
			}
		}
	}
}
