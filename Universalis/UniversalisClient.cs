using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TradeBuddy.Universalis;

namespace TradeBuddy
{
	/// <summary>
	/// Universalis API Client.
	/// </summary>
	public class UniversalisClient
	{
		/// <summary>
		/// Gets market data of an item for a specific world.
		/// </summary>
		/// <param name="itemId">The item ID.</param>
		/// <param name="worldName">The world's name.</param>
		/// <param name="historyCount">Number of entries to fetch from the history.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
		/// <returns>The market data.</returns>

		public static async Task<CurrentlyShownView?> GetMarketData(uint itemId, string worldName, int listCount, int historyCount, CancellationToken cancellationToken)
		{
			var uriBuilder = new UriBuilder($"https://universalis.app/api/v2/{worldName}/{itemId}?listings={listCount}&entries={historyCount}");

			cancellationToken.ThrowIfCancellationRequested();


			var client = new HttpClient();
			var res = await client
				//.SendAsync(new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri),cancellationToken)
				//.GetAsync(uriBuilder.Uri, cancellationToken)
			  .GetStreamAsync(uriBuilder.Uri, cancellationToken)
			  .ConfigureAwait(false);


			cancellationToken.ThrowIfCancellationRequested();

			var parsedRes = await JsonSerializer
			  .DeserializeAsync<CurrentlyShownView>(res, cancellationToken: cancellationToken)
			  .ConfigureAwait(false);

			return parsedRes;
		}

	}
}
