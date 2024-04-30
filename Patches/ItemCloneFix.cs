using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HappyHomeDesigner.Patches
{
	internal class ItemCloneFix
	{
		public static bool suppress_reduce = false;

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
				transpiler: new(typeof(ItemCloneFix), nameof(PatchTryPlace))
			);
		}

		private static IEnumerable<CodeInstruction> PatchTryPlace(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			var il = new CodeMatcher(source, gen);

			il
				.MatchEndForward(
					new(OpCodes.Isinst, typeof(Furniture)),
					new(OpCodes.Brfalse_S)
				)
				.Advance(1)
				.InsertAndAdvance(
					new(OpCodes.Ldc_I4_1),
					new(OpCodes.Stsfld, typeof(ItemCloneFix).GetField(nameof(suppress_reduce)))
				);

			return il.InstructionEnumeration();
		}

		private static void AfterTryPlace()
		{
			suppress_reduce = false;
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
	}
}
