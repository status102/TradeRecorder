﻿using Dalamud.Game.Network;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TradeRecorder.Window;

namespace TradeRecorder
{
	public unsafe class PluginUI : IDisposable
	{
		public readonly static char[] intToHex = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		private Configuration configuration;
		private readonly static int[] blackList = { 673, 379, 521, 572, 113, 241, 280, 169, 504, 642, 911, 365 };

		private readonly static Dictionary<uint, TextureWrap?> iconList = new();
		private readonly static Dictionary<uint, TextureWrap?> hqIconList = new();
		public History History { get; init; }
		//public Trade Trade { get; init; }
		public Trade2 Trade2 { get; init; }
		public Setting Setting { get; init; }
		public TradeRecorder TradeRecorder { get; init; }

		//public RetainerSellList RetainerSellList { get; init; }
		//public ItemSearchResult ItemSearchResult { get; init; }

		public AtkArrayDataHolder* atkArrayDataHolder { get; init; } = null;

		public StreamWriter? networkMessageWriter;

		public unsafe PluginUI(TradeRecorder TradeRecorder, Configuration configuration) {
			this.configuration = configuration;
			this.TradeRecorder = TradeRecorder;
			Trade2 = new(TradeRecorder);
			History = new(TradeRecorder);
			Setting = new(TradeRecorder);

			var atkArrayDataHolder = &Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
			if (atkArrayDataHolder != null && atkArrayDataHolder->StringArrayCount > 0) { this.atkArrayDataHolder = atkArrayDataHolder; }

			TradeRecorder.GameNetwork.NetworkMessage += NetworkMessageDelegate;
		}

		public void Dispose() {
			configuration.Dispose();

			//Trade.Dispose();
			Trade2.Dispose();
			Setting.Dispose();
			History.Dispose();

			TradeRecorder.GameNetwork.NetworkMessage -= NetworkMessageDelegate;

			if (networkMessageWriter != null) {
				networkMessageWriter.Flush();
				networkMessageWriter.Close();
			}
			foreach (TextureWrap? icon in iconList.Values) { icon?.Dispose(); }
			foreach (TextureWrap? icon in hqIconList.Values) { icon?.Dispose(); }
		}

		public void Draw() {
			Trade2.Draw();
			History.Draw();
			Setting.Draw();
			//RetainerSellList.Draw();
			//ItemSearchResult.Draw();
		}

		public unsafe void NetworkMessageDelegate(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
#if DEBUG
			byte[] test = new byte[11];
			Marshal.Copy(dataPtr, test, 0, test.Length);
			if (test[0] == 0 && test.TakeLast(9).All(i => i == 0) && (test[1] == 0 || test[1] == 1)) {
				PluginLog.Information($"疑似窗口包{opCode}：{BitConverter.ToString(test)}");
			}
			if (Array.IndexOf(blackList, opCode) == -1) {
				byte[] bytes = new byte[0x30];
				Marshal.Copy(dataPtr, bytes, 0, bytes.Length);
				PluginLog.Verbose($"OpCode: {opCode}, Direc: {(direction == NetworkMessageDirection.ZoneDown ? "↓" : "↑")}：{BitConverter.ToString(bytes)}");
			}
			if (networkMessageWriter != null) {
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
				for (int i = 0; i < 200; i++) {
					databyte = Marshal.ReadByte(dataPtr, i);
					stringBuilder.Append('-').Append(intToHex[databyte / 16]).Append(intToHex[databyte % 16]);
				}
				networkMessageWriter.WriteLine(stringBuilder.ToString());
				//networkMessageWriter.WriteLine(Encoding.UTF8.GetString(databyte));
			}
#endif
		}

		//public TextureWrap? GetIcon(uint iconId) => GetIcon(iconId, false);
		public static TextureWrap? GetIcon(uint iconId, bool isHq = false) {
			if (iconId == 0) { return null; }
			if (!isHq && iconList.ContainsKey(iconId)) { return iconList[iconId]; }
			if (isHq && hqIconList.ContainsKey(iconId)) { return hqIconList[iconId]; }
			/*
			TextureWrap? icon = isHq ?
				DataManager.GetImGuiTextureHqIcon(iconId) :
				DataManager.GetImGuiTextureIcon(iconId);*/
			TextureWrap? icon = GetIconStr(iconId, isHq);
			if (icon == null) { return null; }
			if (isHq) {
				hqIconList.Add(iconId, icon);
			} else {
				iconList.Add(iconId, icon);
			}
			return icon;
		}
		private static TextureWrap? GetIconStr(uint iconId, bool isHQ) {
			//"ui/icon/{0:D3}000/{1}{2:D6}.tex";
			return DalamudInterface.DataManager.GetImGuiTexture(string.Format("ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex", iconId / 1000u, isHQ ? "hq/" : "", iconId));
		}
	}
}