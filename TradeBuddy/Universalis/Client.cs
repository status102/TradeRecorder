using System;
using System.Threading;
using System.Threading.Tasks;
using TradeBuddy.Universalis.API;

namespace TradeBuddy.Universalis
{
	public class Client
	{
		public static void GetCurrentlyShownView(string dcName, uint itemId, Action<CurrentlyShownView?> action, int listCount = 1, int historyCount = 0,int outOfTime = 30000)
		{
			var cancel = new CancellationTokenSource();
			cancel.Token.Register(() =>
			{
				if (cancel != null) cancel.Dispose();
				cancel = null;
				action(null);
			});
			cancel.CancelAfter(outOfTime);
			Task.Run(async () =>
			{
				var price = await UniversalisClient
					.GetMarketData(itemId, dcName, listCount, historyCount, cancel.Token)
					.ConfigureAwait(false);
				if (cancel != null) cancel.Dispose();
				cancel = null;
				if (price != null && price.itemID != 0 && price.itemID == itemId)
					action(price);
				else
					action(null);
			}, cancel.Token);
		}
	}
}
