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

		public static void Apply(Harmony harmony)
		{
			harmony.TryPatch(
				typeof(Farmer).GetMethod(nameof(Farmer.removeItemFromInventory)), 
				prefix: new(typeof(ItemCloneFix), nameof(RemoveTempItem))
			);
			harmony.TryPatch(
				typeof(Farmer).GetMethod(nameof(Farmer.reduceActiveItemByOne)), 
				prefix: new(typeof(ItemCloneFix), nameof(CheckReduceItem))
			);
			harmony.TryPatch(
				typeof(Utility).GetMethod(nameof(Utility.tryToPlaceItem)), 
				postfix: new(typeof(ItemCloneFix), nameof(AfterTryPlace)),
				prefix: new(typeof(ItemCloneFix), nameof(BeforeTryPlace))
			);
			harmony.TryPatch(
				typeof(Farmer).GetProperty(nameof(Farmer.ActiveObject)).SetMethod,
				prefix: new(typeof(ItemCloneFix), nameof(ReplaceGhostItem))
			);
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
