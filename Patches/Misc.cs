using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley.Objects;
using StardewValley;
using System.Reflection;
using HappyHomeDesigner.Menus;
using StardewValley.Extensions;
using Microsoft.Xna.Framework;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Patches
{
	internal class Misc
	{
		internal static void Apply(Harmony harmony)
		{
			harmony.TryPatch(
				typeof(Furniture).GetMethod("loadDescription", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				postfix: new(typeof(Misc), nameof(EditDescription))
			);

			harmony.TryPatch(
				typeof(Utility).GetMethod(nameof(Utility.isWithinTileWithLeeway)),
				postfix: new(typeof(Misc), nameof(SetFreePlace))
			);

			harmony.TryPatch(
				typeof(Furniture).GetMethod(nameof(Furniture.IsCloseEnoughToFarmer)),
				postfix: new(typeof(Misc), nameof(SetFreePlace))
			);

			harmony.TryPatch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.LowPriorityLeftClick)),
				postfix: new(typeof(Misc), nameof(TryPickupCraftable))
			);
		}

		private static string EditDescription(string original, Furniture __instance)
		{
			if (ItemRegistry.GetDataOrErrorItem(__instance.ItemId).IsErrorItem)
				return original;

			return __instance.ItemId switch
			{
				AssetManager.CATALOGUE_ID => ModEntry.i18n.Get("furniture.Catalogue.desc"),
				AssetManager.COLLECTORS_ID => ModEntry.i18n.Get("furniture.CollectorsCatalogue.desc"),
				AssetManager.DELUXE_ID => ModEntry.i18n.Get("furniture.DeluxeCatalogue.desc"),
				_ => original
			};
		}

		private static bool SetFreePlace(bool free_place_allowed)
			=> free_place_allowed || Catalog.MenuVisible();

		private static bool TryPickupCraftable(bool handled, int x, int y, Farmer who)
		{
			if (handled)
				return true;

			if (Game1.activeClickableMenu != null || !Catalog.MenuVisible())
				return false;

			var where = who.currentLocation;
			Vector2 tile = new(x / 64, y / 64);

			if (where.Objects.TryGetValue(tile, out var obj) && obj.CanBeGrabbed && !obj.isDebrisOrForage())
			{
				if (
					// unused chest OR empty OR a sign
					(obj is Chest chest && CanPickupChest(chest)) || obj.heldObject.Value is null || obj is Sign ||

					// OR a regular machine that does not require input
					(obj.GetType() == typeof(SObject) && obj.HasContextTag("is_machine") && !obj.HasContextTag("machine_input")) ||

					// OR only contains an unused chest
					CanPickupChest(obj.heldObject.Value as Chest)
				)
				{
					if (who.addItemToInventoryBool(obj.getOne(), true))
					{
						Game1.playSound("coin");
						obj.performRemoveAction();
						where.Objects.Remove(tile);

						return true;
					}
				}
			}

			return false;
		}

		private static bool CanPickupChest(Chest chest)
			=> chest != null && chest.isEmpty() && !chest.GetMutex().IsLocked();
	}
}
