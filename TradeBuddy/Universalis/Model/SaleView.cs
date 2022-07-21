namespace TradeBuddy.Universalis.Model
{
	public class SaleView
	{
		// Whether or not the item was high-quality.
		public bool hq { get; set; }
		// The price per unit sold.
		public int pricePerUnit { get; set; }
		// The stack size sold.
		public int quantity { get; set; }
		// The sale time, in seconds since the UNIX epoch.
		public long timestamp { get; set; }
		// Whether or not this was purchased from a mannequin. This may be null.
		public bool? onMannequin { get; set; }
		// The world name, if applicable.
		public string? worldName { get; set; }
		// The world ID, if applicable.
		public int? worldID { get; set; }
		// The buyer name.
		public string? buyerName { get; set; }
		// The total price.
		public int total { get; set; }
	}
}
