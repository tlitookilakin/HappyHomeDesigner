using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using static HappyHomeDesigner.Framework.ModUtilities;

namespace HappyHomeDesigner.Patches
{
	internal class FurnitureAction
	{
		internal static void Apply(HarmonyHelper helper)
		{
			helper
				.With<Furniture>(nameof(Furniture.checkForAction)).Prefix(CheckAction)
				.With(nameof(Furniture.performObjectDropInAction)).Prefix(ApplyFairyDust);
		}

		private static bool ApplyFairyDust(Furniture __instance, Item dropInItem, bool probe, 
			bool returnFalseIfItemConsumed, Farmer who, ref bool __result)
		{
			if (__instance.QualifiedItemId != "(F)" + AssetManager.DELUXE_ID || 
				dropInItem.QualifiedItemId != "(O)872")
				return true;

			__result = true;
			if (probe)
				return false;

			if (who is not null)
			{
				__result = !returnFalseIfItemConsumed;
				who.reduceActiveItemByOne();
			}

			var location = __instance.Location;
			var tile = __instance.TileLocation;
			var size = __instance.sourceRect.Value.Size;
			var Region = new Rectangle(
				(int)tile.X + size.X / 32,
				(int)tile.Y + ((size.Y / 16) - __instance.getTilesHigh()) / 2,
				size.X / 16,
				size.Y / 16
			);

			Game1.playSound("secret1");
			int FlashDelay = 1500;

			Utility.addStarsAndSpirals(location, Region.X, Region.Y + Region.Height - 2, Region.Width, 1, FlashDelay, 50, Color.Magenta);

			DelayedAction.screenFlashAfterDelay(.5f, FlashDelay, "wand");
			DelayedAction.functionAfterDelay(() => {
				Utility.addSprinklesToLocation(location, Region.X, Region.Y, Region.Width, Region.Height, 400, 40, Color.White);
				location.furniture.Remove(__instance);
				Game1.createItemDebris(
					ItemRegistry.Create("(T)" + AssetManager.PORTABLE_ID),
					(tile + new Vector2(.5f)) * Game1.tileSize,
					-1,
					location
				);
			}, FlashDelay);

			return false;
		}

		private static List<string> GetCatalogues(Item i, bool held)
		{
			if (i is null)
				return [];

			return i.QualifiedItemId switch
			{
				"(F)" + AssetManager.CATALOGUE_ID => ["Furniture Catalogue", "Catalogue"],
				"(F)" + AssetManager.COLLECTORS_ID => GetCollectorShops(),
				"(F)" + AssetManager.DELUXE_ID or
				"(O)" + AssetManager.PORTABLE_ID or
				"(T)" + AssetManager.PORTABLE_ID
					=> GetCollectorShops("Furniture Catalogue", "Catalogue"),
				_ => i.GetShop(held) is string s ? [s] : []
			};
		}

		private static bool CheckAction(Furniture __instance, ref bool __result)
		{
			var heldShop = GetCatalogues(__instance.heldObject.Value, true);

			switch (__instance.ItemId)
			{
				// Furniture catalogue
				case "1226":
					if (heldShop.Count == 0)
						return true;

					Catalog.ShowCatalog(["Furniture Catalogue", ..heldShop]);
					break;

				// Wallpaper catalogue
				case "1308":
					if (heldShop.Count == 0)
						return true;

					Catalog.ShowCatalog(["Catalogue", .. heldShop]);
					break;

				// All others
				default:

					// portable catalogue
					if (__instance.heldObject.Value?.ItemId == AssetManager.PORTABLE_ID)
					{
						Catalog.ShowCatalog(heldShop);
					}

					// custom catalogue
					else
					{
						var baseShop = GetCatalogues(__instance, false);
						if (baseShop.Count <= 0)
							return true;

						baseShop.AddRange(heldShop);
						Catalog.ShowCatalog(baseShop);
					}
					break;
			}

			__result = true;
			return false;
		}
	}
}
