using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Menus
{
	internal class BigObjectPage : VariantPage<BigObjectEntry, SObject>
	{
		public BigObjectPage(IEnumerable<ISalable> existing) : 
			base(existing, ModEntry.MOD_ID + "/favorite_craftables", "craftable")
		{
			iconRow = 8;
		}

		public override IReadOnlyList<IGridItem> ApplyFilter()
		{
			return current_filter is 0 ? entries : Favorites;
		}

		public override IEnumerable<BigObjectEntry> GetItemsFrom(IEnumerable<ISalable> source, ICollection<string> favorites)
		{
			var season = Game1.currentLocation.GetSeason();
			var seasonName = season.ToString();

			foreach (var item in source)
				if (item is SObject sobj && item.HasTypeBigCraftable())
					yield return new(sobj, season, seasonName, favorites);
		}

		public override ClickableTextureComponent GetTab()
		{
			return new(new(0, 0, 64, 64), Catalog.MenuTexture, new(48, 48, 16, 16), 4f);
		}
	}
}
