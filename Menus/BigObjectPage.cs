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
		public override BigObjectEntry GetItemFrom(ItemQueryResult item, ICollection<string> favorites)
		{
			var season = Game1.currentLocation.GetSeason();
			var seasonName = season.ToString();

			return new((SObject)item.Item, season, seasonName, favorites);
		}

		public override bool CanAddItem(KeyValuePair<IStyleSet, ItemQueryResult> pair)
		{
			return pair.Value.Item is SObject sobj && sobj.HasTypeBigCraftable();
		}

		/// <inheritdoc/>
		public override void Init()
		{
			Tab = new(new(0, 0, 64, 64), Catalog.MenuTexture, new(48, 48, 16, 16), 4f);
		}
	}
}
