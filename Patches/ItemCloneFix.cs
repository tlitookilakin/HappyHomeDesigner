using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using StarModGen.Utils;

namespace HappyHomeDesigner.Patches
{
	internal class ItemCloneFix
	{
		public static bool suppress_reduce = false;
		private static Furniture heldGhostItem = null;

		public static void Apply(HarmonyHelper helper)
		{
			helper
				.With<Farmer>(nameof(Farmer.removeItemFromInventory)).Prefix(RemoveTempItem)
				.With(nameof(Farmer.reduceActiveItemByOne)).Prefix(CheckReduceItem)
				.WithProperty(nameof(Farmer.ActiveObject), false).Prefix(ReplaceGhostItem)
				.With<Utility>(nameof(Utility.tryToPlaceItem)).Prefix(BeforeTryPlace).Postfix(AfterTryPlace);
		}

		[HarmonyBefore("thimadera.StackEverythingRedux")]
		private static void BeforeTryPlace(Item item)
		{
			suppress_reduce = item is Furniture;
			heldGhostItem = Game1.player.TemporaryItem == item ? item as Furniture : null;
		}

		private static void AfterTryPlace()
		{
			suppress_reduce = false;
			heldGhostItem = null;
		}

		private static bool CheckReduceItem()
		{
			return !suppress_reduce;
		}

		private static bool RemoveTempItem(Farmer __instance, Item which)
		{
			if (Catalog.ActiveMenu.Value is Catalog catalog && ModEntry.config.GiveModifier.IsDown())
			{
				if (__instance.TemporaryItem == which)
					__instance.TemporaryItem = which.getOne();

				else if (!catalog.KnownIds.Contains(which.QualifiedItemId))
					return true;

				else
					__instance.addItemToInventory(which.getOne(), __instance.getIndexOfInventoryItem(which));

				return false;
			}
			else if (__instance.TemporaryItem == which)
			{
				__instance.TemporaryItem = null;
				return false;
			}

			return true;
		}

		private static bool ReplaceGhostItem(Farmer __instance, Item __0)
		{
			if (heldGhostItem == null)
				return true;

			if (__0 == null || __0 == heldGhostItem)
			{
				if (__0 is null)
				{
					if (
						Catalog.ActiveMenu.Value is Catalog catalog &&
						ModEntry.config.GiveModifier.IsDown() &&
						catalog.KnownIds.Contains(__instance.TemporaryItem.QualifiedItemId)
					)
					{
						__instance.TemporaryItem = __instance.TemporaryItem.getOne();
					}
					else
					{
						__instance.TemporaryItem = null;
					}
				}
				return false;
			}

			return true;
		}
	}
}
