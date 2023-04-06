using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeRecorder.Universalis;

namespace TradeRecorder.Model
{
	public class Preset
	{
		public uint Id { get; private init; }
		public uint IconId { get; private init; }
		public string Name { get; private init; }

		public bool Quality { get; private init; }

		public uint SetPrice { get; set; }
		public uint SetCount { get; set; }
		public uint StackPrice { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		public Price.ItemPrice? ItemPrice { get; private init; }

		public Preset(string name, bool quality) {
			var item = DalamudInterface.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(i => i.Name.Equals(name));
			if (item != null) {
				Id = item.RowId;
				Name = name;
				IconId = item.Icon;
				Quality = quality;
				ItemPrice = Price.GetItem(Id);
			}
		}
	}
}
