using System.Collections.Generic;
using System.Text.Json.Serialization;
using TradeBuddy.Universalis.Model;

namespace TradeBuddy.Universalis
{
	public class CurrentlyShownView
	{
		/// <summary>
		/// itemID
		/// </summary>
		//[JsonPropertyName("itemID")]
		public int itemID { get; set; }
		/// <summary>
		/// The world ID, if applicable.
		/// </summary>
		[JsonPropertyName("worldID")]
		public int? worldID { get; set; }
		/// <summary>
		/// The last upload time for this endpoint, in milliseconds since the UNIX epoch
		/// </summary>
		[JsonPropertyName("lastUploadTime")]
		public long lastUploadTime { get; set; }
		/// <summary>
		/// The currently-shown listings.
		/// </summary>
		[JsonPropertyName("listings")]
		public IList<ListingView>? listings { get; set; } = new List<ListingView>();
		/// <summary>
		/// The currently-shown sales.
		/// </summary>
		[JsonPropertyName("recentHistory")]
		public IList<SaleView>? recentHistory { get; set; } = new List<SaleView>();
		/// <summary>
		/// The DC name, if applicable.
		/// </summary>
		[JsonPropertyName("dcName")]
		public string? dcName { get; set; }

		/// <summary>
		/// The average listing price, with outliers removed beyond 3 standard deviations of the mean.
		/// </summary>
		[JsonPropertyName("currentAveragePrice")]
		public float currentAveragePrice { get; set; }
		/// <summary>
		/// The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean.
		/// </summary>
		[JsonPropertyName("currentAveragePriceNQ")]
		public float currentAveragePriceNQ { get; set; }
		/// <summary>
		/// The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean.
		/// </summary>
		[JsonPropertyName("currentAveragePriceHQ")]
		public float currentAveragePriceHQ { get; set; }
		/// <summary>
		/// The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
		/// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
		/// This statistic is more useful in historical queries.
		/// </summary>
		[JsonPropertyName("regularSaleVelocity")]
		public float regularSaleVelocity { get; set; }
		/// <summary>
		/// The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
		/// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
		/// This statistic is more useful in historical queries.
		/// </summary>
		[JsonPropertyName("nqSaleVelocity")]
		public float nqSaleVelocity { get; set; }
		/// <summary>
		/// The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
		/// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
		/// This statistic is more useful in historical queries.
		/// </summary>
		[JsonPropertyName("hqSaleVelocity")]
		public float hqSaleVelocity { get; set; }
		// The average sale price, with outliers removed beyond 3 standard deviations of the mean.
		[JsonPropertyName("averagePrice")]
		public float averagePrice { get; set; }
		// The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.
		[JsonPropertyName("averagePriceNQ")]
		public float averagePriceNQ { get; set; }
		// The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean.
		[JsonPropertyName("averagePriceHQ")]
		public float averagePriceHQ { get; set; }
		// The minimum listing price.
		[JsonPropertyName("minPrice")]
		public int minPrice { get; set; }
		// The minimum NQ listing price.
		[JsonPropertyName("minPriceNQ")]
		public int minPriceNQ { get; set; }
		// The minimum HQ listing price.
		[JsonPropertyName("minPriceHQ")]
		public int minPriceHQ { get; set; }
		// The maximum listing price.
		[JsonPropertyName("maxPrice")]
		public int maxPrice { get; set; }
		// The maximum NQ listing price.
		[JsonPropertyName("maxPriceNQ")]
		public int maxPriceNQ { get; set; }
		// The maximum HQ listing price.
		[JsonPropertyName("maxPriceHQ")]
		public int maxPriceHQ { get; set; }
		// A map of quantities to listing counts, representing the number of listings of each quantity.
		[JsonPropertyName("stackSizeHistogram")]
		public Dictionary<string, int>? stackSizeHistogram { get; set; } = new();
		// A map of quantities to NQ listing counts, representing the number of listings of each quantity.
		[JsonPropertyName("stackSizeHistogramNQ")]
		public Dictionary<string, int>? stackSizeHistogramNQ { get; set; } = new();
		// A map of quantities to HQ listing counts, representing the number of listings of each quantity.
		[JsonPropertyName("stackSizeHistogramHQ")]
		public Dictionary<string, int>? stackSizeHistogramHQ { get; set; } = new();
		// The world name, if applicable.
		[JsonPropertyName("worldName")]
		public string? worldName { get; set; }
		// The last upload times in milliseconds since epoch for each world in the response, if this is a DC request.
		[JsonPropertyName("worldUploadTimes")]
		public Dictionary<string, long>? worldUploadTimes { get; set; } = new();
	}
}
