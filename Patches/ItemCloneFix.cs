using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;

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
			if (__instance.TemporaryItem != which)
				return true;
			else
				__instance.TemporaryItem = null;

			return false;
		}

		private static bool ReplaceGhostItem(Farmer __instance, Item __0)
		{
			if (heldGhostItem == null)
				return true;

			if (__0 == null || __0 == heldGhostItem)
			{
				__instance.TemporaryItem = __0;
				return false;
			}

			return true;
		}
	}
}
