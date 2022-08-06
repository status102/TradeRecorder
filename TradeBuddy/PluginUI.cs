using Dalamud.Game.Network;
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
		public readonly static char[] intToHex = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		private Configuration configuration;
		public History History { get; init; }
		public Trade Trade { get; init; }
		public Setting Setting { get; init; }
		public TradeBuddy TradeBuddy { get; init; }

		public RetainerSellList RetainerSellList { get; init; }
		public ItemSearchResult ItemSearchResult { get; init; }

		public AtkArrayDataHolder* atkArrayDataHolder { get; init; } = null;

		private bool visible = false;
		public bool Visible
		{
			get => this.visible;
			set { this.visible = value; }
		}

		private bool settingsVisible = false;
		public bool SettingsVisible
		{
			get => this.settingsVisible;
			set { this.settingsVisible = value; }
		}

		public bool historyVisible = false;//交易历史界面是否可见
		/// <summary>
		/// 交易时候的二次确认
		/// </summary>
		public bool twiceCheck = false;

		public StreamWriter? networkMessageWriter;

		public unsafe PluginUI(TradeBuddy TradeBuddy, Configuration configuration)
		{
			this.configuration = configuration;
			this.TradeBuddy = TradeBuddy;
			Trade = new (TradeBuddy);
			History = new (TradeBuddy);
			Setting = new (TradeBuddy);
			RetainerSellList = new (TradeBuddy);
			ItemSearchResult = new (TradeBuddy);

			var atkArrayDataHolder = &Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
			if (atkArrayDataHolder != null && atkArrayDataHolder->StringArrayCount > 0) this.atkArrayDataHolder = atkArrayDataHolder;

			TradeBuddy.GameNetwork.NetworkMessage += networkMessageDelegate;

		}

		public void Dispose()
		{
			configuration.Dispose();

			Trade.Dispose();
			Setting.Dispose();
			History.Dispose();

			TradeBuddy.GameNetwork.NetworkMessage -= networkMessageDelegate;

			if (networkMessageWriter != null)
			{
				networkMessageWriter.Flush();
				networkMessageWriter.Close();
			}
		}

		public void Draw()
		{
			Trade.Draw(configuration.ShowTrade, ref twiceCheck, ref historyVisible, ref settingsVisible);
			History.Draw(ref historyVisible);
			Setting.Draw(ref settingsVisible);
			RetainerSellList.Draw();
			ItemSearchResult.Draw();
		}

		public unsafe void networkMessageDelegate(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
		{
			if (networkMessageWriter != null)
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
					stringBuilder.Append('-').Append(intToHex[databyte / 16]).Append(intToHex[databyte % 16]);
				}
				networkMessageWriter.WriteLine(stringBuilder.ToString());
				//networkMessageWriter.WriteLine(Encoding.UTF8.GetString(databyte));
			}
		}
	}
}
