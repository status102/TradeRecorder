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
	{
		private static ConcurrentDictionary<string, int> custom = new();
		private static CancellationTokenSource? cancel;
		private static readonly object obj = new object();
		private static int earn = 0;
		public static void MessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (type.ToString() == "2110" && message.TextValue.Contains("获得了"))
			{
				var reg = new Regex("(?:)(?<item>.+).×(?<count>\\d+)");
				var match = reg.Match(message.TextValue);
				if (match.Success)
				{
					var name = match.Groups["item"].Value;
					var count = int.Parse(match.Groups["count"].Value);
					if (!string.IsNullOrEmpty(name))
					{
						if (custom.ContainsKey(name)) custom[name] += count;
						else custom[name] = count;
						calculate();
					}
				}
				DalamudDll.ChatGui.Print($"{type}；{message.TextValue}");
			}
		}

		private static void calculate()
		{
			Monitor.Enter(obj);
			if (cancel != null) cancel.Cancel();

			cancel = new CancellationTokenSource();
			cancel.Token.Register(() => { });
			cancel.CancelAfter(TimeSpan.FromSeconds(20));
			Task.Run(() =>
			{
				Monitor.Enter(obj);
				cancel.Token.ThrowIfCancellationRequested();
				earn = 0;
				foreach (var item in custom)
				{
					var itemByName = DalamudDll.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(r => r.Name == item.Key);
					if (itemByName != null)
					{
						earn += (int)itemByName.PriceLow * item.Value;
					}
				}
				var str = string.Join('，', custom.Select(pair => $"{pair.Key}x{pair.Value}"));
				DalamudDll.ChatGui.Print($"总收益：{earn}，物品：{str}");
				Monitor.Exit(obj);
			}, cancel.Token);
			Monitor.Exit(obj);
		}
	}
}
#endif