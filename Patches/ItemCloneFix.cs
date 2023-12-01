using HarmonyLib;
using StardewValley;
using System;

namespace HappyHomeDesigner.Patches
{
	internal class ItemCloneFix
	{
		public static void Apply(Harmony harmony)
		{
			harmony.Patch(typeof(Farmer).GetMethod(nameof(Farmer.reduceActiveItemByOne)), new(typeof(ItemCloneFix), nameof(Prefix)));
		}

		private static bool Prefix(Farmer __instance)
		{
			if (__instance.TemporaryItem is null)
				return true;

			if (--__instance.TemporaryItem.Stack is <= 0)
				__instance.TemporaryItem = null;

			return false;
		}
	}
}
