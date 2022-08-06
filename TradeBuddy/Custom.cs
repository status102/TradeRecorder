#if DEBUG
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TradeBuddy
{
	/// <summary>
	/// 自定义的一些玩意，杂七杂八的
	/// </summary>
	public class Custom
	{//[653]<6774843>获得12x1；[424]<5777056>
		private static readonly int[] TerritoryType = { 424, 423, 425, 653 };// 部队工坊地图ID 沙, 海, 森, 白
		private static ConcurrentDictionary<string, int> custom = new();
		private static CancellationTokenSource? cancel;
		private static readonly object obj = new();
		//private static int earn = 0;
		private TradeBuddy _tradeBuddy { get; init; }

		public Custom(TradeBuddy tradeBuddy) {
			_tradeBuddy = tradeBuddy;
			DalamudDll.ChatGui.ChatMessage += MessageDelegate;
			DalamudDll.ClientState.TerritoryChanged += TerritoryChangedDelegate;
		}
		public void MessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
			if (type.ToString() == "2110" && message.TextValue.Contains("获得了") && TerritoryType.ToList().Contains(DalamudDll.ClientState.TerritoryType)) {
				var reg = new Regex("(?:)(?<item>.+).×(?<count>\\d+)");
				var match = reg.Match(message.TextValue);
				if (match.Success) {
					var name = match.Groups["item"].Value;
					var count = int.Parse(match.Groups["count"].Value);
					if (!string.IsNullOrEmpty(name)) {
						DalamudDll.ChatGui.Print($"[{DalamudDll.ClientState.TerritoryType}]<{DalamudDll.ClientState.LocalPlayer?.TargetObjectId}>得{name}x{count}");
						if (custom.ContainsKey(name))
							custom[name] += count;
						else
							custom[name] = count;
						calculate();
					}
				}
			} else if (type == XivChatType.Echo && message.TextValue.Contains("测试：")) {
				var reg = new Regex("(?:)(?<item>.+).×(?<count>\\d+)");
				var match = reg.Match(message.TextValue);
				if (match.Success) {
					var name = match.Groups["item"].Value;
					var count = int.Parse(match.Groups["count"].Value);
					if (!string.IsNullOrEmpty(name)) {
						DalamudDll.ChatGui.Print($"[{DalamudDll.ClientState.TerritoryType}]<{DalamudDll.ClientState.LocalPlayer?.TargetObjectId}>获得{name}x{count}");
						if (custom.ContainsKey(name))
							custom[name] += count;
						else
							custom[name] = count;
						calculate();
					}
				}
			}
		}

		private void calculate() => calculate(TimeSpan.FromSeconds(180));
		private void calculate(TimeSpan delay) => calculate(delay, TimeSpan.FromSeconds(600));
		private void calculate(TimeSpan delay, TimeSpan outTime) {
			if (custom.IsEmpty)
				return;
			lock (obj) {
				if (Custom.cancel != null) {
					try {
						Custom.cancel.Cancel();
					} catch (ObjectDisposedException) { }
					Custom.cancel.Dispose();
					Custom.cancel = null;
				}
				if (custom.IsEmpty)
					return;

				var cancel = new CancellationTokenSource();
				cancel.Token.Register(() => { cancel = null; });
				cancel.CancelAfter(outTime);
				Task.Delay(delay, cancel.Token).ContinueWith(_ =>
				{
					cancel.Token.ThrowIfCancellationRequested();
					lock (obj) {
						var earn = 0;
						foreach (var item in custom) {
							var itemByName = DalamudDll.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(r => r.Name == item.Key.Replace("", ""));
							if (itemByName != null) {
								//PriceLow是卖给NPC的价格，PriceMid是NPC卖给玩家的价格。HQ是NQ价格x 1.1向上取整
								if (item.Key.Contains("")) {
									earn += (int)Math.Ceiling(1.1 * itemByName.PriceLow) * item.Value;
								} else {
									earn += (int)itemByName.PriceLow * item.Value;
								}
							}
						}
						var str = string.Join('，', custom.Select(pair => $"{pair.Key}x{pair.Value}"));
						DalamudDll.ChatGui.Print($"总收益：{earn:#,##0}，物品：{str}");
						custom.Clear();
						cancel.Dispose();
						cancel = null;
					}
				}, cancel.Token);
				Custom.cancel = cancel;
			}
		}

		public void TerritoryChangedDelegate(object? sender, ushort e) {
			calculate(TimeSpan.Zero);
		}

		public void Dispose() {
			DalamudDll.ChatGui.ChatMessage -= MessageDelegate;
			DalamudDll.ClientState.TerritoryChanged -= TerritoryChangedDelegate;
		}
	}
}
#endif