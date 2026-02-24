using HappyHomeDesigner.Data;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Internal;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Menus
{
	internal class BigObjectPage : VariantPage<BigObjectEntry, SObject>
	{
		public BigObjectPage() : base(ModEntry.MOD_ID + "/favorite_craftables", "craftable")
		{
			iconRow = 64;
			filter_count = 2;
		}

		/// <inheritdoc/>
		public override IReadOnlyList<IGridItem> ApplyFilter()
			=> current_filter is 0 ? entries : Favorites;

		/// <inheritdoc/>
		public override IEnumerable<KeyValuePair<IStyleSet, BigObjectEntry>> GetItemsFrom(IEnumerable<KeyValuePair<IStyleSet, ItemQueryResult>> source, ICollection<string> favorites)
		{
			var season = Game1.currentLocation.GetSeason();
			var seasonName = season.ToString();

			foreach ((var shop, var item) in source)
				if (item.Item is SObject sobj && sobj.HasTypeBigCraftable())
					yield return new(shop, new(sobj, season, seasonName, favorites));
		}

		/// <inheritdoc/>
		public override void Init()
		{
			Tab = new(new(0, 0, 64, 64), Catalog.MenuTexture, new(48, 48, 16, 16), 4f);
		}
	}
}
