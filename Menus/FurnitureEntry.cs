﻿using HappyHomeDesigner.Integration;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Menus
{
	internal class FurnitureEntry : VariantEntry<Furniture>
	{
		/// <inheritdoc/>
		public FurnitureEntry(Furniture Item, Season season, string seasonName, ICollection<string> favorites)
			: base(Item, season, seasonName, favorites, "Furniture_")
		{
		}

		/// <inheritdoc/>
		public FurnitureEntry(Furniture Item)
			: base(Item)
		{
			Item.currentRotation.Value = 0;
			Item.updateRotation();
		}

		/// <inheritdoc/>
		public override Furniture GetOne()
		{
			var item = Item.getOne() as Furniture;
			item.Price = 0;
			item.currentRotation.Value = 0;
			item.updateRotation();
			return item;
		}

		/// <inheritdoc/>
		public override IReadOnlyList<VariantEntry<Furniture>> GetVariants()
		{
			if (!HasVariants)
				return new[] { new FurnitureEntry(Item) };

			List<Furniture> skins = [Item];
			AlternativeTextures.VariantsOfFurniture(Item, season, skins);

			return skins.Select(f => new FurnitureEntry(f) as VariantEntry<Furniture>).ToList();
		}

		/// <inheritdoc/>
		public override bool CanPlace()
		{
			if (Item is BedFurniture bed)
			{
				var location = Game1.currentLocation;

				if (!bed.CanModifyBed(Game1.player))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Bed_CantMoveOthersBeds"));
					return false;
				}

				if (location is FarmHouse house)
				{
					if (house.upgradeLevel < (int)bed.bedType)
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Bed_NeedsUpgrade"));
						return false;
					}
				}
			}

			return true;
		}
	}
}
