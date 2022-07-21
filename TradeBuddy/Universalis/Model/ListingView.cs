using System.Collections.Generic;

namespace TradeBuddy.Universalis.Model
{
	public class ListingView
	{
		// The time that this listing was posted, in seconds since the UNIX epoch.
		public long lastReviewTime { get; set; }
		// The price per unit sold.
		public int pricePerUnit { get; set; }
		// The stack size sold.
		public int quantity { get; set; }
		// The ID of the dye on this item.
		public int stainID { get; set; }
		// The world name, if applicable.
		public string? worldName { get; set; }
		// The world ID, if applicable.
		public int? worldID { get; set; }
		// The creator's character name.
		public string? creatorName { get; set; }
		// A SHA256 hash of the creator's ID.
		public string? creatorID { get; set; }
		// Whether or not the item is high-quality.
		public bool hq { get; set; }
		// Whether or not the item is crafted.
		public bool isCrafted { get; set; }
		// A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null.
		public string? listingID { get; set; }
		// The materia on this item.
		public IList<MateriaView>? materia { get; set; } = new List<MateriaView>();
		// Whether or not the item is being sold on a mannequin.
		public bool onMannequin { get; set; }
		// The city ID of the retainer.
		// Limsa Lominsa = 1
		// Gridania = 2
		// Ul'dah = 3
		// Ishgard = 4
		// Kugane = 7
		// Crystarium = 10
		public int retainerCity { get; set; }
		// A SHA256 hash of the retainer's ID.
		public string? retainerID { get; set; }
		// The retainer's name.
		public string? retainerName { get; set; }
		// A SHA256 hash of the seller's ID.
		public string? sellerID { get; set; }
		// The total price.
		public int total { get; set; }

		public class MateriaView
		{
			// The materia slot.
			public int slotID { get; set; }
			// The materia item ID.
			public int materiaID { get; set; }
		}
	}
}
